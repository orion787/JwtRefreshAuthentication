using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Tresh.Api.Dtos;
using Tresh.Dal;
using Tresh.Domain.Models;
using Tresh.Domain.Validations;

namespace Tresh.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToDoController: ControllerBase
    {
        private DataContext _context {  get; set; }
        public ToDoController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            var item = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);

            if(item == null)
                return NotFound();

            return Ok(item);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllItems()
        {
            var items = await _context.Items.ToListAsync();

            if(items == null)
                return NotFound();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody]ItemDataDto newItem)
        {
            var item = new ItemData
            {
                Description = newItem.Description,
                Title = newItem.Title,
                Done = newItem.Done
            };

            _context.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateItem), new { id = item.Id }, item);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> UpdateItem([FromBody]ItemDataDto newItem, int id)
        {
            var updated = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);

            if(updated == null)
                return NotFound();

            updated.Description = newItem.Description;
            updated.Title = newItem.Title;
            updated.Done = newItem.Done;

            if (ItemDataValidation.Check(updated))
            {
                _context.Update(updated);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound();

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
