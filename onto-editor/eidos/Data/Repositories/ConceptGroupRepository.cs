using Eidos.Models;
using Microsoft.EntityFrameworkCore;

namespace Eidos.Data.Repositories
{
    public interface IConceptGroupRepository
    {
        Task<List<ConceptGroup>> GetByOntologyAndUserAsync(int ontologyId, string userId);
        Task<ConceptGroup?> GetByIdAsync(int id);
        Task<ConceptGroup?> GetByParentConceptAsync(int ontologyId, int parentConceptId, string userId);
        Task<ConceptGroup> CreateAsync(ConceptGroup group);
        Task<ConceptGroup> UpdateAsync(ConceptGroup group);
        Task DeleteAsync(int id);
    }

    public class ConceptGroupRepository : IConceptGroupRepository
    {
        private readonly OntologyDbContext _context;
        private readonly ILogger<ConceptGroupRepository> _logger;

        public ConceptGroupRepository(OntologyDbContext context, ILogger<ConceptGroupRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ConceptGroup>> GetByOntologyAndUserAsync(int ontologyId, string userId)
        {
            return await _context.ConceptGroups
                .Where(g => g.OntologyId == ontologyId && g.UserId == userId)
                .Include(g => g.ParentConcept)
                .ToListAsync();
        }

        public async Task<ConceptGroup?> GetByIdAsync(int id)
        {
            return await _context.ConceptGroups
                .Include(g => g.ParentConcept)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<ConceptGroup?> GetByParentConceptAsync(int ontologyId, int parentConceptId, string userId)
        {
            return await _context.ConceptGroups
                .FirstOrDefaultAsync(g => g.OntologyId == ontologyId
                    && g.ParentConceptId == parentConceptId
                    && g.UserId == userId);
        }

        public async Task<ConceptGroup> CreateAsync(ConceptGroup group)
        {
            group.CreatedAt = DateTime.UtcNow;
            group.UpdatedAt = DateTime.UtcNow;

            _context.ConceptGroups.Add(group);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created concept group {GroupId} for ontology {OntologyId}",
                group.Id, group.OntologyId);

            return group;
        }

        public async Task<ConceptGroup> UpdateAsync(ConceptGroup group)
        {
            group.UpdatedAt = DateTime.UtcNow;

            _context.ConceptGroups.Update(group);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated concept group {GroupId}", group.Id);

            return group;
        }

        public async Task DeleteAsync(int id)
        {
            var group = await _context.ConceptGroups.FindAsync(id);
            if (group != null)
            {
                _context.ConceptGroups.Remove(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted concept group {GroupId}", id);
            }
        }
    }
}
