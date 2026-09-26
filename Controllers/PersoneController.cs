using GestionalePersone.Data;
using GestionalePersone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersoneController : ControllerBase
{
    private readonly AppDbContext _context;

    public PersoneController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Persona>>> GetPersone()
    {
        return await _context.Persone
            .OrderBy(p => p.Cognome)
            .ThenBy(p => p.Nome)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Persona>> GetPersona(int id)
    {
        var persona = await _context.Persone.FindAsync(id);

        if (persona == null)
            return NotFound();

        return persona;
    }

    [HttpPost]
    public async Task<ActionResult<Persona>> CreatePersona(Persona persona)
    {
        _context.Persone.Add(persona);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPersona),
            new { id = persona.Id },
            persona);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePersona(
        int id,
        Persona persona)
    {
        if (id != persona.Id)
            return BadRequest();

        var esistente = await _context.Persone.FindAsync(id);

        if (esistente == null)
            return NotFound();

        esistente.Nome = persona.Nome;
        esistente.Cognome = persona.Cognome;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePersona(int id)
    {
        var persona = await _context.Persone.FindAsync(id);

        if (persona == null)
            return NotFound();

        _context.Persone.Remove(persona);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
