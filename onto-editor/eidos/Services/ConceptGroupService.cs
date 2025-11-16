using Eidos.Data.Repositories;
using Eidos.Models;
using System.Text.Json;

namespace Eidos.Services
{
    public interface IConceptGroupService
    {
        Task<List<ConceptGroup>> GetGroupsForOntologyAsync(int ontologyId, string userId);
        Task<ConceptGroup> CreateGroupAsync(int ontologyId, string userId, int parentConceptId, List<int> childConceptIds);
        Task<ConceptGroup> AddToGroupAsync(int groupId, int conceptId);
        Task<ConceptGroup> RemoveFromGroupAsync(int groupId, int conceptId);
        Task<ConceptGroup> ToggleCollapseAsync(int groupId);
        Task DeleteGroupAsync(int groupId);
        Task<bool> CanNestGroup(int ontologyId, string userId, int parentConceptId, int maxDepth = 5);
        Task<bool> CanCreateGroupAsync(int ontologyId, string userId, int parentConceptId, List<int> childConceptIds);
    }

    public class ConceptGroupService : IConceptGroupService
    {
        private readonly IConceptGroupRepository _repository;
        private readonly IRelationshipRepository _relationshipRepository;
        private readonly ILogger<ConceptGroupService> _logger;

        public ConceptGroupService(
            IConceptGroupRepository repository,
            IRelationshipRepository relationshipRepository,
            ILogger<ConceptGroupService> logger)
        {
            _repository = repository;
            _relationshipRepository = relationshipRepository;
            _logger = logger;
        }

        public async Task<List<ConceptGroup>> GetGroupsForOntologyAsync(int ontologyId, string userId)
        {
            return await _repository.GetByOntologyAndUserAsync(ontologyId, userId);
        }

        public async Task<ConceptGroup> CreateGroupAsync(int ontologyId, string userId, int parentConceptId, List<int> childConceptIds)
        {
            // Prevent circular group creation
            var allGroups = await _repository.GetByOntologyAndUserAsync(ontologyId, userId);

            // Check 1: None of the child concepts can already be a parent in another group
            foreach (var childId in childConceptIds)
            {
                var existingGroupWithChildAsParent = allGroups.FirstOrDefault(g => g.ParentConceptId == childId);
                if (existingGroupWithChildAsParent != null)
                {
                    _logger.LogWarning("Cannot create group: concept {ChildId} is already a parent in group {GroupId}",
                        childId, existingGroupWithChildAsParent.Id);
                    throw new InvalidOperationException($"Concept {childId} is already a parent in another group. Cannot create circular reference.");
                }
            }

            // Check 2: The parent concept cannot already be a child in any group where one of the new children is the parent
            foreach (var existingGroup in allGroups)
            {
                var groupChildIds = JsonSerializer.Deserialize<List<int>>(existingGroup.ChildConceptIds) ?? new List<int>();
                if (groupChildIds.Contains(parentConceptId) && childConceptIds.Contains(existingGroup.ParentConceptId))
                {
                    _logger.LogWarning("Cannot create group: would create circular reference between {ParentId} and {ChildId}",
                        parentConceptId, existingGroup.ParentConceptId);
                    throw new InvalidOperationException($"Cannot create group: would create circular reference between concepts {parentConceptId} and {existingGroup.ParentConceptId}.");
                }
            }

            // Check if group already exists for this parent
            var existing = await _repository.GetByParentConceptAsync(ontologyId, parentConceptId, userId);
            if (existing != null)
            {
                // Add to existing group
                var existingIds = JsonSerializer.Deserialize<List<int>>(existing.ChildConceptIds) ?? new List<int>();
                existingIds.AddRange(childConceptIds.Where(id => !existingIds.Contains(id)));
                existing.ChildConceptIds = JsonSerializer.Serialize(existingIds);
                existing.IsCollapsed = true;  // Ensure group is collapsed when adding new children

                // Re-analyze relationships with the new children
                var updatedRelationships = await AnalyzeRelationshipsForGrouping(parentConceptId, existingIds);
                existing.CollapsedRelationships = JsonSerializer.Serialize(updatedRelationships);

                return await _repository.UpdateAsync(existing);
            }

            // Analyze relationships for all concepts being grouped
            var collapsedRelationships = await AnalyzeRelationshipsForGrouping(parentConceptId, childConceptIds);

            // Create new group
            var group = new ConceptGroup
            {
                OntologyId = ontologyId,
                UserId = userId,
                ParentConceptId = parentConceptId,
                ChildConceptIds = JsonSerializer.Serialize(childConceptIds),
                CollapsedRelationships = JsonSerializer.Serialize(collapsedRelationships),
                IsCollapsed = true,
                MaxDepth = 5
            };

            _logger.LogInformation(
                "Creating concept group with parent {ParentId}, {ChildCount} children, and {RelationshipCount} tracked relationships",
                parentConceptId, childConceptIds.Count, collapsedRelationships.Count);

            return await _repository.CreateAsync(group);
        }

        public async Task<ConceptGroup> AddToGroupAsync(int groupId, int conceptId)
        {
            var group = await _repository.GetByIdAsync(groupId);
            if (group == null)
                throw new InvalidOperationException($"Group {groupId} not found");

            var childIds = JsonSerializer.Deserialize<List<int>>(group.ChildConceptIds) ?? new List<int>();
            if (!childIds.Contains(conceptId))
            {
                childIds.Add(conceptId);
                group.ChildConceptIds = JsonSerializer.Serialize(childIds);
                await _repository.UpdateAsync(group);
            }

            return group;
        }

        public async Task<ConceptGroup> RemoveFromGroupAsync(int groupId, int conceptId)
        {
            var group = await _repository.GetByIdAsync(groupId);
            if (group == null)
                throw new InvalidOperationException($"Group {groupId} not found");

            var childIds = JsonSerializer.Deserialize<List<int>>(group.ChildConceptIds) ?? new List<int>();
            childIds.Remove(conceptId);

            if (childIds.Count == 0)
            {
                // Delete group if empty
                await _repository.DeleteAsync(groupId);
                throw new InvalidOperationException("Group deleted (empty)");
            }

            group.ChildConceptIds = JsonSerializer.Serialize(childIds);
            return await _repository.UpdateAsync(group);
        }

        public async Task<ConceptGroup> ToggleCollapseAsync(int groupId)
        {
            var group = await _repository.GetByIdAsync(groupId);
            if (group == null)
                throw new InvalidOperationException($"Group {groupId} not found");

            // If the group is currently collapsed and we're expanding it, delete it entirely
            if (group.IsCollapsed)
            {
                _logger.LogInformation("Deleting group {GroupId} (was expanded)", groupId);
                await _repository.DeleteAsync(groupId);
                return group; // Return the group before deletion for UI update
            }

            // Otherwise, collapse the group
            group.IsCollapsed = true;
            return await _repository.UpdateAsync(group);
        }

        public async Task DeleteGroupAsync(int groupId)
        {
            await _repository.DeleteAsync(groupId);
        }

        public async Task<bool> CanNestGroup(int ontologyId, string userId, int parentConceptId, int maxDepth = 5)
        {
            // Check nesting depth to prevent infinite loops
            int depth = await GetNestingDepth(ontologyId, userId, parentConceptId);
            return depth < maxDepth;
        }

        public async Task<bool> CanCreateGroupAsync(int ontologyId, string userId, int parentConceptId, List<int> childConceptIds)
        {
            try
            {
                // Perform all the same validation checks as CreateGroupAsync, but without actually creating
                var allGroups = await _repository.GetByOntologyAndUserAsync(ontologyId, userId);

                // Check 1: None of the child concepts can already be a parent in another group
                foreach (var childId in childConceptIds)
                {
                    var existingGroupWithChildAsParent = allGroups.FirstOrDefault(g => g.ParentConceptId == childId);
                    if (existingGroupWithChildAsParent != null)
                    {
                        _logger.LogDebug("Cannot create group: concept {ChildId} is already a parent in group {GroupId}",
                            childId, existingGroupWithChildAsParent.Id);
                        return false;
                    }
                }

                // Check 2: The parent concept cannot already be a child in any group where one of the new children is the parent
                foreach (var existingGroup in allGroups)
                {
                    var groupChildIds = JsonSerializer.Deserialize<List<int>>(existingGroup.ChildConceptIds) ?? new List<int>();
                    if (groupChildIds.Contains(parentConceptId) && childConceptIds.Contains(existingGroup.ParentConceptId))
                    {
                        _logger.LogDebug("Cannot create group: would create circular reference between {ParentId} and {ChildId}",
                            parentConceptId, existingGroup.ParentConceptId);
                        return false;
                    }
                }

                // Check 3: Nesting depth
                if (!await CanNestGroup(ontologyId, userId, parentConceptId))
                {
                    _logger.LogDebug("Cannot create group: maximum nesting depth exceeded for {ParentId}", parentConceptId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating group creation for parent {ParentId}", parentConceptId);
                return false;
            }
        }

        /// <summary>
        /// Analyzes all relationships involving the concepts being grouped.
        /// Returns metadata about which relationships need to be hidden vs rerouted.
        /// </summary>
        private async Task<List<CollapsedRelationshipInfo>> AnalyzeRelationshipsForGrouping(int parentConceptId, List<int> childConceptIds)
        {
            var collapsedRelationships = new List<CollapsedRelationshipInfo>();
            var allGroupedConceptIds = new HashSet<int>(childConceptIds) { parentConceptId };

            // Get all relationships for each concept in the group
            foreach (var conceptId in allGroupedConceptIds)
            {
                var relationships = await _relationshipRepository.GetByConceptIdAsync(conceptId);

                foreach (var relationship in relationships)
                {
                    // Skip if we've already tracked this relationship
                    if (collapsedRelationships.Any(r => r.RelationshipId == relationship.Id))
                        continue;

                    var isFromGrouped = allGroupedConceptIds.Contains(relationship.SourceConceptId);
                    var isToGrouped = allGroupedConceptIds.Contains(relationship.TargetConceptId);

                    // Determine if this is an internal or external relationship
                    int? externalConceptId = null;
                    if (isFromGrouped && !isToGrouped)
                    {
                        externalConceptId = relationship.TargetConceptId;
                    }
                    else if (!isFromGrouped && isToGrouped)
                    {
                        externalConceptId = relationship.SourceConceptId;
                    }

                    var info = new CollapsedRelationshipInfo
                    {
                        RelationshipId = relationship.Id,
                        FromConceptId = relationship.SourceConceptId,
                        ToConceptId = relationship.TargetConceptId,
                        IsFromGroupedChild = isFromGrouped,
                        IsToGroupedChild = isToGrouped,
                        ExternalConceptId = externalConceptId,
                        RelationshipType = relationship.RelationType
                    };

                    collapsedRelationships.Add(info);

                    _logger.LogDebug(
                        "Tracked relationship {RelationshipId}: {From} -> {To} (Type: {Type}, External: {External}, ShouldReroute: {Reroute})",
                        relationship.Id, relationship.SourceConceptId, relationship.TargetConceptId,
                        relationship.RelationType, externalConceptId, info.ShouldBeRerouted);
                }
            }

            return collapsedRelationships;
        }

        private async Task<int> GetNestingDepth(int ontologyId, string userId, int conceptId, int currentDepth = 0)
        {
            // Safety check to prevent infinite recursion
            if (currentDepth >= 100)
            {
                _logger.LogWarning("Maximum nesting depth (100) reached for concept {ConceptId} in ontology {OntologyId}. Possible circular reference.",
                    conceptId, ontologyId);
                return currentDepth;
            }

            // Find if this concept is inside any group
            var groups = await _repository.GetByOntologyAndUserAsync(ontologyId, userId);

            foreach (var group in groups)
            {
                var childIds = JsonSerializer.Deserialize<List<int>>(group.ChildConceptIds) ?? new List<int>();
                if (childIds.Contains(conceptId))
                {
                    // This concept is inside a group, recurse to check its parent
                    // Prevent checking the same concept again (would be a circular reference)
                    if (group.ParentConceptId == conceptId)
                    {
                        _logger.LogWarning("Circular reference detected: concept {ConceptId} is its own parent in group {GroupId}",
                            conceptId, group.Id);
                        return currentDepth;
                    }

                    return await GetNestingDepth(ontologyId, userId, group.ParentConceptId, currentDepth + 1);
                }
            }

            return currentDepth;
        }
    }
}
