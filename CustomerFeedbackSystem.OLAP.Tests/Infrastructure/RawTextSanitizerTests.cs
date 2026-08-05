using System.Text;
using CustomerFeedbackSystem.OLAP.Infrastructure.Text;
using FluentAssertions;

namespace CustomerFeedbackSystem.OLAP.Tests.Infrastructure;

/// <summary>
/// Every string in the system passes through this class, so it gets the most attention.
/// The "unchanged" cases matter most: they verify the sanitizer does not do TOO MUCH.
/// Each character it wrongly stripped would be signal the T phase never gets back.
/// <para>
/// Non-ASCII inputs are built from explicit code points rather than typed literals, so an
/// editor silently re-encoding this file cannot quietly defeat an assertion.
/// </para>
/// </summary>
public sealed class RawTextSanitizerTests
{
    private const char CombiningAcute = '́';
    private const string PrecomposedEAcute = "é";

    private readonly RawTextSanitizer _sanitizer = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_WhenValueIsAbsent_ReturnsSentinel(string? value)
    {
        _sanitizer.Sanitize(value, 50).Value.Should().Be("-");
    }

    [Fact]
    public void Sanitize_WithCustomSentinel_UsesIt()
    {
        // Interacciones_Raw is semantically numeric, so it uses "0" instead of "-".
        _sanitizer.Sanitize(null, 20, "0").Value.Should().Be("0");
    }

    [Fact]
    public void Sanitize_TrimsSurroundingWhitespace()
    {
        _sanitizer.Sanitize("  hola  ", 50).Value.Should().Be("hola");
    }

    [Fact]
    public void Sanitize_CollapsesRepeatedWhitespace()
    {
        _sanitizer.Sanitize("hola\t\tmundo", 50).Value.Should().Be("hola mundo");
    }

    [Fact]
    public void Sanitize_RemovesControlCharactersWithoutLeavingAGap()
    {
        var value = "hola" + (char)0 + "mundo";

        _sanitizer.Sanitize(value, 50).Value.Should().Be("holamundo");
    }

    [Fact]
    public void Sanitize_LeavesAccentsUntouched()
    {
        // In Spanish "esta" and "está" are different words.
        var value = "El café está frío";

        _sanitizer.Sanitize(value, 50).Value.Should().Be(value);
    }

    [Fact]
    public void Sanitize_LeavesEnyeAndCasingUntouched()
    {
        // Casing is signal for the T phase: MALO is not malo.
        var value = "El NIÑO y la niña";

        _sanitizer.Sanitize(value, 50).Value.Should().Be(value);
    }

    [Fact]
    public void Sanitize_LeavesPunctuationUntouched()
    {
        // "!" and "?" carry sentiment intensity.
        var value = "¡Pésimo!";

        _sanitizer.Sanitize(value, 50).Value.Should().Be(value);
    }

    [Fact]
    public void Sanitize_LeavesEmojiUntouched()
    {
        var value = "Excelente " + char.ConvertFromUtf32(0x1F60D);

        _sanitizer.Sanitize(value, 50).Value.Should().Be(value);
    }

    [Fact]
    public void Sanitize_NormalizesToFormC()
    {
        // "e" + U+0301 must become the precomposed "é", or two visually identical comments
        // would compare as different and break the T phase's deduplication.
        var decomposed = "e" + CombiningAcute;
        decomposed.Should().HaveLength(2);

        var result = _sanitizer.Sanitize(decomposed, 50);

        result.Value.Should().Be(PrecomposedEAcute);
        result.Value.Should().HaveLength(1);
        result.Value.IsNormalized(NormalizationForm.FormC).Should().BeTrue();
    }

    [Fact]
    public void Sanitize_WhenLongerThanTheColumn_TruncatesAndReportsIt()
    {
        var result = _sanitizer.Sanitize("abcdefghij", 5);

        result.Value.Should().Be("abcde");
        result.WasTruncated.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_WhenItFits_DoesNotReportTruncation()
    {
        _sanitizer.Sanitize("abc", 50).WasTruncated.Should().BeFalse();
    }

    [Fact]
    public void Sanitize_WithZeroMaxLength_NeverTruncates()
    {
        // NVARCHAR(MAX) columns pass 0.
        var value = new string('a', 10_000);

        var result = _sanitizer.Sanitize(value, 0);

        result.Value.Should().HaveLength(10_000);
        result.WasTruncated.Should().BeFalse();
    }

    [Fact]
    public void Sanitize_NeverSplitsASurrogatePair()
    {
        // Cutting blindly at maxLength would slice the emoji in half and store a replacement
        // character.
        var value = "abcd" + char.ConvertFromUtf32(0x1F60D);

        var result = _sanitizer.Sanitize(value, 5);

        result.Value.Should().Be("abcd");
        result.WasTruncated.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_WhenOnlyControlCharactersRemain_ReturnsSentinel()
    {
        // Nothing survives the strip, so the sentinel applies rather than an empty string —
        // every staging column is NOT NULL.
        var value = new string([(char)1, (char)2, (char)3]);

        _sanitizer.Sanitize(value, 50).Value.Should().Be("-");
    }
}
