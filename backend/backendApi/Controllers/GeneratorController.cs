
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc; 
// using Application.Documents.Commands; 

// namespace backendApi.Controllers;

// [ApiController]
// [Route("api/[controller]")]
// public class GeneratorController : ControllerBase
// {

//     private readonly GenerateDocumentationCommand _command; 

//     public GeneratorController(GenerateDocumentationCommand command)
//     {
//         _command = command; 
//     }
    
//     // [Authorize]
//     [HttpPost("upload")]
//     public async Task<IActionResult> Upload(
//         IFormFile file, [FromForm] string outputType
//     )
//     {
//         if(file == null || file.Length == 0)
//             return BadRequest("File is required"); 

//         var result = await _command.ExecuteAsync(file, outputType); 

//         return Ok(result); 
//     }
// }