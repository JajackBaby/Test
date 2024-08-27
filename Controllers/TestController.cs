using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Test.Controllers
{
    [Route("")]
    [ApiController]
    public class TestController : ControllerBase
    {
        // POST /customer/{customerid}/score/{score}  
        [HttpPost("customer/{customerid}/score/{score}")]
        public IActionResult UpdateCustomerScore(long customerid, int score)
        {
            var customer = Customer.Instance;
            var newScore = customer.UpdateCustomerScore(customerid, score);

            // 返回更新后的分数  
            return Ok(JsonSerializer.Serialize(new { CustomerId = customerid, Score = newScore }));
        }

        //GET /leaderboard?start={start}&end={end}
        [HttpGet("leaderboard")]
        public IActionResult GetCustomersByRank([FromQuery] int start, [FromQuery] int end)
        {
            var customer = Customer.Instance;
            var result = customer.GetCustomersByRank(start,end);

            // 返回结果  
            return Ok(JsonSerializer.Serialize(result));
        }

        // GET /leaderboard/{customerid}?high={high}&low={low}  
        [HttpGet("leaderboard/{customerid}")]
        public IActionResult GetCustomerWithNeighborsById(long customerid, [FromQuery] int high, [FromQuery] int low)
        {
            var customer = Customer.Instance;
            var result = customer.GetCustomerWithNeighborsById(customerid, high, low);
            return Ok(JsonSerializer.Serialize(result));
        }
    }
}
