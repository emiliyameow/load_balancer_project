// Контроллер отключён — заменён на Minimal API в BackendApiEndpoints.cs
// (RuntimeBackendRegistry + MapBackendApi)
// Conflict: [Route("api/[controller]")] совпадал с маршрутами Minimal API.

#if false

using LoadBalancer.API.Api.DTO;
using LoadBalancer.API.Balance;
using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.ServiceCache;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Immutable;
using LoadBalancer.API.Rout;

namespace LoadBalancer.API.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class BackendController : ControllerBase
{   
    private readonly ServiceCacheHandler _serviceCache;
    private readonly HealthCache _healthCache;
    private readonly BackendLoadTracker _loadTracker;

    public BackendController(
        ServiceCacheHandler serviceCache,
        HealthCache healthCache,
        BackendLoadTracker loadTracker)
    {
        _serviceCache = serviceCache;
        _healthCache = healthCache;
        _loadTracker = loadTracker;
    }

    [HttpGet]
    public ActionResult<List<ServerDTO>> GetAll()
    {
       var services = _serviceCache.GetAll();
       
       var result = new List<ServerDTO>();
       
       foreach (var service in services)
       {
           foreach (var item in service.Value)
           {
               result.Add(ToDto(service.Key, item));
           }
       }
       return result;
    }

    [HttpPost]
    public ActionResult<ServerDTO> Add([FromBody] CreateServerDTO dto)
    {
        var serverCondition = new ServerCondition
        {
            ServerInfo = new BackendConfig()
            {
                Port = dto.Port,
                Name = dto.Name,
                Host = dto.Host
            },
            IsAlive = true,
            Weight = dto.Weight
        };

        var currentInstances = _serviceCache
            .GetInstances(dto.ServiceName)
            .ToImmutableList();

        var updatedInstances = currentInstances.Add(serverCondition);

        _serviceCache.AddOrUpdateService(dto.ServiceName, updatedInstances);

        var result = ToDto(dto.ServiceName, serverCondition);

        return CreatedAtAction(nameof(GetAll), result);
    }


    [HttpPatch("update")]
    public ActionResult UpdateServer([FromBody] UpdateServerDTO dto)
    {
        var validationResult = ValidateUpdateServerDto(dto);

        if (validationResult is not null)
            return validationResult;

        var updated = _serviceCache.UpdateServer(
            dto.ServiceName,
            dto.Name,
            dto.Address,
            dto.Host,
            dto.Port,
            dto.Weight
        );

        if (!updated)
            return NotFound("Server not found");

        return NoContent();
    }
    private ActionResult? ValidateUpdateServerDto(UpdateServerDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ServiceName))
            return BadRequest("ServiceName is required");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required");

        if (dto.Address is not null && string.IsNullOrWhiteSpace(dto.Address))
            return BadRequest("Address cannot be empty");

        if (dto.Host is not null && string.IsNullOrWhiteSpace(dto.Host))
            return BadRequest("Host cannot be empty");

        if (dto.Port.HasValue && (dto.Port.Value <= 0 || dto.Port.Value > 65535))
            return BadRequest("Invalid port");

        if (dto.Weight.HasValue && dto.Weight.Value <= 0)
            return BadRequest("Weight must be greater than 0");

        return null;
    }

    private ServerDTO ToDto(string serviceName, ServerCondition item)
    {
        var address = item.ServerInfo.Address;
        var hasHealth = _healthCache.TryGet(address, out var health);
        var activeRequests = _loadTracker.GetActiveRequests(address);
        var weight = hasHealth ? health.Weight : item.Weight;
        var error = hasHealth ? health.Error : "Health check pending";

        if (hasHealth && !health.IsAlive && string.IsNullOrWhiteSpace(error))
            error = "Backend is not healthy";

        return new ServerDTO
        {
            Address = address,
            Port = item.ServerInfo.Port,
            IsAlive = hasHealth ? health.IsAlive : item.IsAlive,
            Name = item.ServerInfo.Name,
            Host = item.ServerInfo.Host,
            ServiceName = serviceName,
            Weight = weight,
            BalancerActiveRequests = activeRequests,
            EffectiveWeight = weight + activeRequests,
            LatencyMs = hasHealth ? health.LatencyMs : null,
            CheckedAt = hasHealth ? health.CheckedAt : null,
            Error = error
        };
    }


}

#endif
