using _01_WebApiAutores.DTOs;
using _01_WebApiAutores.Entidades;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _01_WebApiAutores.Controllers
{
    [ApiController]
    [Route("api/libros")]
    public class LibrosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public LibrosController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("{id:int}", Name ="ObtenerLibro")]
        public async Task<ActionResult<LibroDTO>> Get(int id)
        {
            var libro = await _context.Libros
                .Include(LibroDTO => LibroDTO.AutoresLibros)
                .ThenInclude(autorLibroDB => autorLibroDB.Autor)
                .Include(libroBD => libroBD.Comentarios)
                .FirstOrDefaultAsync(x => x.Id == id);

            //aplicando orden de los autores
            libro.AutoresLibros = libro.AutoresLibros.OrderBy(x => x.Orden).ToList();

            return _mapper.Map<LibroDTO>(libro);
        }

        [HttpPost]
        public async Task<ActionResult> Post(LibroCreacionDTO libroCreacionDTO)
        {
            if (libroCreacionDTO.AutoresIds == null)
            {
                return BadRequest("No se puede crear un libro sin autores");
            }

            //Validando que exista el autor que el usuario selecciono parar registrarlo con el Libro
            var autores = await _context.Autores.Where(autorBD => libroCreacionDTO.AutoresIds.Contains(autorBD.Id)).ToListAsync();

            var autoresIds = autores.Select(x => x.Id).ToList();

            //mostrar error si los conteos son diferentes
            if (libroCreacionDTO.AutoresIds.Count != autoresIds.Count)
            {
                return BadRequest("No existe uno de los autores enviados");
            }

            var libro = _mapper.Map<Libro>(libroCreacionDTO);

            //Agregando logica para colocar el orden de como se va agregando los autores
            if (libro.AutoresLibros != null)
            {
                for (int i = 0; i < libro.AutoresLibros.Count; i++)
                {
                    libro.AutoresLibros[i].Orden = i;
                }
            }

            _context.Add(libro);
            await _context.SaveChangesAsync();
            //return Ok();

            var libroDTO = _mapper.Map<LibroDTO>(libro);
            return CreatedAtRoute("ObtenerLibro", new { id = libro.Id }, libroDTO);
        }
    }
}
