﻿using RdwTechdayRegistration.Data;
using RdwTechdayRegistration.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace RdwTechdayRegistration.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TijdvakkenController : Controller
    {
        private readonly RdwTechdayRegistration.Data.ApplicationDbContext _context;

        public TijdvakkenController(RdwTechdayRegistration.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tijdvakken
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tijdvakken
                .OrderBy(t => t.Order)
                .ToListAsync());
        }

        // GET: Tijdvakken/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tijdvak = await _context.Tijdvakken
                .SingleOrDefaultAsync(m => m.Id == id);
            if (tijdvak == null)
            {
                return NotFound();
            }

            return View(tijdvak);
        }

        // GET: Tijdvakken/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tijdvakken/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Order,Start,Einde")] Tijdvak tijdvak)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tijdvak);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tijdvak);
        }

        // GET: Tijdvakken/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tijdvak = await _context.Tijdvakken.SingleOrDefaultAsync(m => m.Id == id);
            if (tijdvak == null)
            {
                return NotFound();
            }
            return View(tijdvak);
        }

        // POST: Tijdvakken/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Order,Start,Einde")] Tijdvak tijdvak)
        {
            if (id != tijdvak.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tijdvak);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TijdvakExists(tijdvak.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tijdvak);
        }

        // GET: Tijdvakken/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tijdvak = await _context.Tijdvakken
                .SingleOrDefaultAsync(m => m.Id == id);
            if (tijdvak == null)
            {
                return NotFound();
            }

            return View(tijdvak);
        }

        // POST: Tijdvakken/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tijdvak = await _context.Tijdvakken.SingleOrDefaultAsync(m => m.Id == id);
            _context.Tijdvakken.Remove(tijdvak);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TijdvakExists(int id)
        {
            return _context.Tijdvakken.Any(e => e.Id == id);
        }
    }
}
