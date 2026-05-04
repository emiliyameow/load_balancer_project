using LoadBalancer.API.Balance;
using Microsoft.AspNetCore.Mvc;

namespace LoadBalancer.API.Api.Controllers;

[ApiController]
[Route("api")]
public class BalancingController : ControllerBase
{
    private readonly BalanceAlgoritm _balanceAlgoritm;

    public BalancingController(BalanceAlgoritm balanceAlgoritm)
    {
        _balanceAlgoritm = balanceAlgoritm;
    }

    [HttpPatch("change-balancing-algorithm/{algorithm}")]
    public ActionResult ChangeBalancingAlgorithm(string algorithm)
    {
        var changed = _balanceAlgoritm.TrySetStrategy(algorithm);

        if (!changed)
            return BadRequest("Unknown balancing algorithm");

        return Ok(new
        {
            currentAlgorithm = _balanceAlgoritm.CurrentAlgorithm
        });
    }

    [HttpGet("balancing-algorithm")]
    public ActionResult GetCurrentBalancingAlgorithm()
    {
        return Ok(new
        {
            currentAlgorithm = _balanceAlgoritm.CurrentAlgorithm,
            availableAlgorithms = _balanceAlgoritm.GetAvailableAlgorithms()
        });
    }
}