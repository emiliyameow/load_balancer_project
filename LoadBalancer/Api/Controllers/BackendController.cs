using LoadBalancer.API.Api.DTO;
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

    public BackendController(ServiceCacheHandler serviceCache)
    {
        _serviceCache = serviceCache;
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
               var server = new ServerDTO
               {
                   Address = item.ServerInfo.Address,
                   Port = item.ServerInfo.Port,
                   IsAlive = item.IsAlive,
                   Name = item.ServerInfo.Name,
                   Host = item.ServerInfo.Host,
                   ServiceName = service.Key,
                   Weight = item.Weight
               };
               result.Add(server);
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

        var result = new ServerDTO
        {
            Address = dto.Address,
            Port = dto.Port,
            IsAlive = true,
            Name = dto.Name,
            Host = dto.Host,
            ServiceName = dto.ServiceName,
            Weight = dto.Weight
        };

        return CreatedAtAction(nameof(GetAll), result);
    }


    [HttpPatch("update")]
    public ActionResult UpdateServer([FromBody] UpdateServerDTO dto)
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


}