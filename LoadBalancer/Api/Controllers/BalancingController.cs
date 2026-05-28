using LoadBalancer.API.Balance;
using Microsoft.AspNetCore.Mvc;

namespace LoadBalancer.API.Api.Controllers;

[ApiController]
[Route("api")]
public class BalancingController : ControllerBase
{
    private readonly BalanceAlgorithm _balanceAlgorithm;

    public BalancingController(BalanceAlgorithm balanceAlgorithm)
    {
        _balanceAlgorithm = balanceAlgorithm;
    }

    [HttpPatch("change-balancing-algorithm/{algorithm}")]
    public ActionResult ChangeBalancingAlgorithm(string algorithm)
    {
        var changed = _balanceAlgorithm.TrySetStrategy(algorithm);

        if (!changed)
            return BadRequest("Unknown balancing algorithm");

        return Ok(new
        {
            currentAlgorithm = _balanceAlgorithm.CurrentAlgorithm
        });
    }

    [HttpGet("balancing-algorithm")]
    public ActionResult GetCurrentBalancingAlgorithm()
    {
        return Ok(new
        {
            currentAlgorithm = _balanceAlgorithm.CurrentAlgorithm,
            availableAlgorithms = _balanceAlgorithm.GetAvailableAlgorithms()
        });
    }
}