using GestionalePersone.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionalePersone.Controllers;

// Elenco dei moduli dell'app e dei loro livelli (per le pagine di Admin).
[ApiController]
[Authorize]
[Route("api/moduli")]
public class ModuliController : ControllerBase
{
    [HttpGet]
    public IEnumerable<DefinizioneModulo> GetModuli() => Moduli.Tutti;
}
