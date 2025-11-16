using Eidos.Services;
using Xunit;

namespace Eidos.Tests.Unit.Services;

/// <summary>
/// Unit tests for WikiLinkParser
/// Tests Obsidian-style [[wiki-link]] parsing functionality
/// </summary>
public class WikiLinkParserTests
{
    private readonly WikiLinkParser _parser;

    public WikiLinkParserTests()
    {
        _parser = new WikiLinkParser();
    }

    #region ExtractConceptNames Tests

    [Fact]
    public void ExtractConceptNames_EmptyContent_ReturnsEmptyList()
    {
        // Arrange
        var content = "";

        // Act
        var result = _parser.ExtractConceptNames(content);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractConceptNames_NullContent_ReturnsEmptyList()
    {
        // Arrange
        string? content = null;

        // Act
        var result = _parser.ExtractConceptNames(content!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractConceptNames_SingleSimpleLink_ReturnsConceptName()
    {
        // Arrange
        var content = "This is a note about [[Person]].";

        // Act
        var result = _parser.ExtractConceptNames(content);

        // Assert
        Assert.Single(result);
        Assert.Equal("Person", result[0]);
    }

    [Fact]
    public void ExtractConceptNames_MultipleLinks_ReturnsAllConceptNames()
    {
        // Arrange
        var content = "[[Person]] works at [[Organization]] in [[Location]].";

        // Act
        var result = _parser.ExtractConceptNames(content);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("Person", result);
        Assert.Contains("Organization", result);
        Assert.Contains("Location", result);
    }

    [Fact]
    public void ExtractConceptNames_LinkWithDisplayText_ExtractsConceptNameOnly()
    {
        // Arrange
        var content = "[[Person|John Doe]] is the founder.";

        // Act
        var result = _parser.ExtractConceptNames(content);

        // Assert
        Assert.Single(result);
        Assert.Equal("Person", result[0]);
    }

    [Fact]
    public void ExtractConceptNames_DuplicateLinks_ReturnsDistinctNames()
    {
        // Arrange
        var content = "[[Person]] and [[Person]] are related to [[Person]].";

        // Act
        var result = _parser.ExtractConceptNames(content);

        // Assert
        Assert.Single(result);
        Assert.Equal("Person", result[0]);
    }

    [Fact]
    public void ExtractConceptNames_LinkWithSpaces_TrimsConceptName()
    {
        // Arrange
        var content = "[[ Person ]] with extra spaces.";

        // Act
        var result = _parser.ExtractConceptNames(content);

        // Assert
        Assert.Single(result);
        Assert.Equal("Person", result[0]);
    }

    [Fact]
    public void ExtractConceptNames_MultiWordConcept_PreservesSpaces()
    {
        // Arrange
        var content = "[[Project Management]] and [[Software Engineering]].";

        // Act
        var result = _parser.ExtractConceptNames(content);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Project Management", result);
        Assert.Contains("Software Engineering", result);
    }

    #endregion

    #region ExtractLinksWithContext Tests

    [Fact]
    public void ExtractLinksWithContext_SimpleLink_ReturnsLinkWithContext()
    {
        // Arrange
        var content = "This is a note about [[Person]].";

        // Act
        var result = _parser.ExtractLinksWithContext(content);

        // Assert
        Assert.Single(result);
        var link = result[0];
        Assert.Equal("Person", link.ConceptName);
        Assert.Null(link.DisplayText);
        Assert.Equal(21, link.Position);
        Assert.Equal("[[Person]]", link.MatchedText);
        Assert.Contains("note about", link.ContextSnippet);
    }

    [Fact]
    public void ExtractLinksWithContext_LinkWithDisplayText_ParsesBothParts()
    {
        // Arrange
        var content = "[[Person|John Doe]] is the founder.";

        // Act
        var result = _parser.ExtractLinksWithContext(content);

        // Assert
        Assert.Single(result);
        var link = result[0];
        Assert.Equal("Person", link.ConceptName);
        Assert.Equal("John Doe", link.DisplayText);
        Assert.Equal("[[Person|John Doe]]", link.MatchedText);
    }

    [Fact]
    public void ExtractLinksWithContext_MultipleLinks_ReturnsAllWithPositions()
    {
        // Arrange
        var content = "[[First]] and [[Second]] concepts.";

        // Act
        var result = _parser.ExtractLinksWithContext(content);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[0].ConceptName);
        Assert.Equal(0, result[0].Position);
        Assert.Equal("Second", result[1].ConceptName);
        Assert.Equal(14, result[1].Position);
    }

    [Fact]
    public void ExtractLinksWithContext_LongContent_TruncatesContext()
    {
        // Arrange
        var longPrefix = new string('a', 100);
        var longSuffix = new string('b', 100);
        var content = $"{longPrefix}[[Person]]{longSuffix}";

        // Act
        var result = _parser.ExtractLinksWithContext(content);

        // Assert
        Assert.Single(result);
        var link = result[0];
        Assert.StartsWith("...", link.ContextSnippet);
        Assert.EndsWith("...", link.ContextSnippet);
    }

    [Fact]
    public void ExtractLinksWithContext_EmptyConceptName_SkipsLink()
    {
        // Arrange
        var content = "[[]] is empty.";

        // Act
        var result = _parser.ExtractLinksWithContext(content);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region CountLinks Tests

    [Fact]
    public void CountLinks_EmptyContent_ReturnsZero()
    {
        // Arrange
        var content = "";

        // Act
        var result = _parser.CountLinks(content);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountLinks_SingleLink_ReturnsOne()
    {
        // Arrange
        var content = "[[Person]]";

        // Act
        var result = _parser.CountLinks(content);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void CountLinks_MultipleLinks_ReturnsCorrectCount()
    {
        // Arrange
        var content = "[[First]] [[Second]] [[Third]]";

        // Act
        var result = _parser.CountLinks(content);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void CountLinks_NoLinks_ReturnsZero()
    {
        // Arrange
        var content = "This has no wiki links.";

        // Act
        var result = _parser.CountLinks(content);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region ContainsLinks Tests

    [Fact]
    public void ContainsLinks_WithLinks_ReturnsTrue()
    {
        // Arrange
        var content = "[[Person]] is here.";

        // Act
        var result = _parser.ContainsLinks(content);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsLinks_NoLinks_ReturnsFalse()
    {
        // Arrange
        var content = "No links here.";

        // Act
        var result = _parser.ContainsLinks(content);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsLinks_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var content = "";

        // Act
        var result = _parser.ContainsLinks(content);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ReplaceLinks Tests

    [Fact]
    public void ReplaceLinks_SimpleReplacement_ReplacesLink()
    {
        // Arrange
        var content = "[[Person]] is here.";

        // Act
        var result = _parser.ReplaceLinks(content, link => $"<{link.ConceptName}>");

        // Assert
        Assert.Equal("<Person> is here.", result);
    }

    [Fact]
    public void ReplaceLinks_MultipleLinks_ReplacesAllInOrder()
    {
        // Arrange
        var content = "[[First]] and [[Second]]";

        // Act
        var result = _parser.ReplaceLinks(content, link => $"<{link.ConceptName}>");

        // Assert
        Assert.Equal("<First> and <Second>", result);
    }

    [Fact]
    public void ReplaceLinks_WithDisplayText_UsesDisplayTextInReplacement()
    {
        // Arrange
        var content = "[[Person|John Doe]] is here.";

        // Act
        var result = _parser.ReplaceLinks(content, link =>
            link.DisplayText != null ? link.DisplayText : link.ConceptName);

        // Assert
        Assert.Equal("John Doe is here.", result);
    }

    [Fact]
    public void ReplaceLinks_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var content = "";

        // Act
        var result = _parser.ReplaceLinks(content, link => "REPLACED");

        // Assert
        Assert.Equal("", result);
    }

    #endregion

    #region ConvertLinksToHtml Tests

    [Fact]
    public void ConvertLinksToHtml_SimpleLink_GeneratesHtmlAnchor()
    {
        // Arrange
        var content = "[[Person]]";

        // Act
        var result = _parser.ConvertLinksToHtml(content);

        // Assert
        Assert.Contains("<a href=\"/concept/Person\"", result);
        Assert.Contains("class=\"wiki-link\"", result);
        Assert.Contains("data-concept=\"Person\"", result);
        Assert.Contains(">Person</a>", result);
    }

    [Fact]
    public void ConvertLinksToHtml_LinkWithDisplayText_UsesDisplayText()
    {
        // Arrange
        var content = "[[Person|John Doe]]";

        // Act
        var result = _parser.ConvertLinksToHtml(content);

        // Assert
        Assert.Contains(">John Doe</a>", result);
        Assert.Contains("data-concept=\"Person\"", result);
    }

    [Fact]
    public void ConvertLinksToHtml_CustomUrlTemplate_UsesTemplate()
    {
        // Arrange
        var content = "[[Person]]";

        // Act
        var result = _parser.ConvertLinksToHtml(content, "/ontology/{0}");

        // Assert
        Assert.Contains("<a href=\"/ontology/Person\"", result);
    }

    [Fact]
    public void ConvertLinksToHtml_ConceptWithSpaces_EscapesUrl()
    {
        // Arrange
        var content = "[[Project Management]]";

        // Act
        var result = _parser.ConvertLinksToHtml(content);

        // Assert
        Assert.Contains("Project%20Management", result);
    }

    #endregion

    #region IsValidConceptName Tests

    [Fact]
    public void IsValidConceptName_ValidName_ReturnsTrue()
    {
        // Arrange
        var name = "Person";

        // Act
        var result = _parser.IsValidConceptName(name);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidConceptName_NameWithSpaces_ReturnsTrue()
    {
        // Arrange
        var name = "Project Management";

        // Act
        var result = _parser.IsValidConceptName(name);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidConceptName_EmptyName_ReturnsFalse()
    {
        // Arrange
        var name = "";

        // Act
        var result = _parser.IsValidConceptName(name);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidConceptName_NullName_ReturnsFalse()
    {
        // Arrange
        string? name = null;

        // Act
        var result = _parser.IsValidConceptName(name!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidConceptName_NameWithBrackets_ReturnsFalse()
    {
        // Arrange
        var name = "Person[Test]";

        // Act
        var result = _parser.IsValidConceptName(name);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidConceptName_NameWithPipe_ReturnsFalse()
    {
        // Arrange
        var name = "Person|Display";

        // Act
        var result = _parser.IsValidConceptName(name);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidConceptName_NameWithNewline_ReturnsFalse()
    {
        // Arrange
        var name = "Person\nTest";

        // Act
        var result = _parser.IsValidConceptName(name);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region EscapeConceptName Tests

    [Fact]
    public void EscapeConceptName_ValidName_ReturnsUnchanged()
    {
        // Arrange
        var name = "Person";

        // Act
        var result = _parser.EscapeConceptName(name);

        // Assert
        Assert.Equal("Person", result);
    }

    [Fact]
    public void EscapeConceptName_NameWithBrackets_RemovesBrackets()
    {
        // Arrange
        var name = "Person[Test]";

        // Act
        var result = _parser.EscapeConceptName(name);

        // Assert
        Assert.Equal("PersonTest", result);
    }

    [Fact]
    public void EscapeConceptName_NameWithPipe_ReplacesPipeWithDash()
    {
        // Arrange
        var name = "Person|Display";

        // Act
        var result = _parser.EscapeConceptName(name);

        // Assert
        Assert.Equal("Person-Display", result);
    }

    [Fact]
    public void EscapeConceptName_NameWithNewlines_ReplacesWithSpace()
    {
        // Arrange
        var name = "Person\nTest\rName";

        // Act
        var result = _parser.EscapeConceptName(name);

        // Assert
        Assert.Equal("Person Test Name", result);
    }

    [Fact]
    public void EscapeConceptName_NameWithMultipleInvalidChars_CleansAll()
    {
        // Arrange
        var name = "[Person]|Test\n";

        // Act
        var result = _parser.EscapeConceptName(name);

        // Assert
        Assert.Equal("Person-Test", result);
    }

    [Fact]
    public void EscapeConceptName_EmptyName_ReturnsEmpty()
    {
        // Arrange
        var name = "";

        // Act
        var result = _parser.EscapeConceptName(name);

        // Assert
        Assert.Equal("", result);
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void ExtractConceptNames_NestedBrackets_HandlesCorrectly()
    {
        // Arrange - Should not match nested brackets as they're invalid
        var content = "[[Person [with brackets]]]";

        // Act
        var result = _parser.ExtractConceptNames(content);

        // Assert
        // The regex should match "Person [with brackets" before the last ]]
        Assert.Single(result);
    }

    [Fact]
    public void ExtractLinksWithContext_LinkAtStart_NoLeadingEllipsis()
    {
        // Arrange
        var content = "[[Person]] at the start.";

        // Act
        var result = _parser.ExtractLinksWithContext(content);

        // Assert
        Assert.Single(result);
        Assert.DoesNotContain("...", result[0].ContextSnippet.Substring(0, 3));
    }

    [Fact]
    public void ExtractLinksWithContext_LinkAtEnd_NoTrailingEllipsis()
    {
        // Arrange
        var content = "At the end is [[Person]]";

        // Act
        var result = _parser.ExtractLinksWithContext(content);

        // Assert
        Assert.Single(result);
        var snippet = result[0].ContextSnippet;
        Assert.DoesNotContain("...", snippet.Substring(snippet.Length - 3));
    }

    [Fact]
    public void ExtractLinksWithContext_MultilineContent_CollapsesWhitespace()
    {
        // Arrange
        var content = @"This is a
            multi-line note
            about [[Person]].";

        // Act
        var result = _parser.ExtractLinksWithContext(content);

        // Assert
        Assert.Single(result);
        // Context should have collapsed whitespace
        Assert.DoesNotContain("\n", result[0].ContextSnippet);
        Assert.Contains("multi-line", result[0].ContextSnippet);
    }

    [Fact]
    public void ConvertLinksToHtml_MultipleLinksInSentence_PreservesOrder()
    {
        // Arrange
        var content = "[[Person]] works at [[Organization]].";

        // Act
        var result = _parser.ConvertLinksToHtml(content);

        // Assert
        var personIndex = result.IndexOf(">Person</a>");
        var orgIndex = result.IndexOf(">Organization</a>");
        Assert.True(personIndex < orgIndex, "Person link should appear before Organization link");
    }

    [Fact]
    public void ExtractConceptNames_SpecialCharactersInConcept_ExtractsCorrectly()
    {
        // Arrange
        var content = "[[Person-Name]] and [[Project_2024]]";

        // Act
        var result = _parser.ExtractConceptNames(content);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Person-Name", result);
        Assert.Contains("Project_2024", result);
    }

    #endregion
}
