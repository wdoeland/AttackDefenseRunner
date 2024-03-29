using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AttackDefenseRunner.Model;
using AttackDefenseRunner.Util;
using AttackDefenseRunner.Util.Docker;
using AttackDefenseRunner.Util.Parsing;
using AttackDefenseRunner.Util.Parsing.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AttackDefenseRunner.Controllers
{
    [ApiController]
    public class DockerImageController : Controller
    {
        private readonly IDockerImageManager _imageManager;
        private readonly ADRContext _context;
        private readonly DockerImageJsonParser _parser;

        public DockerImageController(IDockerImageManager imageManager, DockerImageJsonParser parser, ADRContext context)
        {
            _imageManager = imageManager;
            _parser = parser;
            _context = context;
        }

        [HttpGet(Endpoint.CONTAINER_BASE)]
        public async Task<List<DockerContainer>> GetContainers()
            => await _context
                .DockerContainers
                .Include(c => c.DockerTag)
                .ThenInclude(t => t.DockerImage)
                .ToListAsync();

        [HttpPost(Endpoint.UPDATE_IMAGE)]
        public async Task<DockerContainer> UpdateImage()
        {
            DockerTagJson dockerTagJson;
            
            try
            {
                dockerTagJson = await _parser.ParseDockerJsonTag(Request.Body);
            }
            catch (ParseFailedException e)
            {
                Log.Error("JSON Parse failed {e}", e);
                return null;
            }
            
            var container = await _imageManager.UpdateImage(dockerTagJson.Tag);

            // Fetch related Tag and Image as well
            return await _context
                .DockerContainers
                .Include(c => c.DockerTag)
                .ThenInclude(t => t.DockerImage)
                .Where(c => c.Id == container.Id)
                .FirstAsync();
        }

        [HttpPost(Endpoint.IMAGE_BASE + Endpoint.ID + Endpoint.STOP)]
        public async Task StopImage(string id)
        {
            await _imageManager.StopImage(id);
        }
        
        [HttpPost(Endpoint.CONTAINER_BASE + Endpoint.ID + Endpoint.STOP)]
        public async Task StopContainer(string id)
        {
            await _imageManager.StopContainer(id);
        }
    }
}