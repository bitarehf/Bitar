using Bitar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bitar.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PersonsController : ControllerBase
    {
        private readonly BitarContext _context;

        public PersonsController(BitarContext context)
        {
            _context = context;
        }

        // GET: api/Persons
        [HttpGet]
        public List<Person> GetPersons()
        {
            return _context.Persons.ToList();
        }

        // GET: api/Persons/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Person>> GetPerson(string id)
        {
            return await _context.Persons.FindAsync(id);
        }

        // POST: api/Persons
        /// <summary>
        /// Creates person if person doesn't exist
        /// otherwise just updates person BitcoinAddress
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Person>> CreateAsync(Person person)
        {
            if (await _context.Persons.AnyAsync(c => c.SSN == person.SSN) == false) 
            {
                _context.Persons.Add(person);
            }
            else
            {
                _context.Persons.Update(person);
            }

            await _context.SaveChangesAsync();

            return person;
        }
    }
}