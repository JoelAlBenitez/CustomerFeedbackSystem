using CustomerFeedbackSystem.OLAP.Infrastructure.Extraction.Database;
using FluentAssertions;

namespace CustomerFeedbackSystem.OLAP.Tests.Infrastructure;

/// <summary>
/// String assertions on a SQL constant, which sounds weak until you notice they catch the
/// two most likely mistakes in this extractor: dropping the ñ from the table name, and
/// "correcting" the double-m typo in IdCommentario. Either one makes the query fail at
/// runtime against the real database.
/// </summary>
public sealed class WebReviewQueryTests
{
    [Fact]
    public void Sql_BracketsTheTableNameThatCarriesAnEnye()
    {
        WebReviewQuery.Sql.Should().Contain("dbo.[Reseñas]");
    }

    [Fact]
    public void Sql_KeepsTheDoubleMTypoThatExistsInTheRealSchema()
    {
        WebReviewQuery.Sql.Should().Contain("r.IdCommentario");
    }

    [Fact]
    public void Sql_UsesOuterApplySoAMissingWebSourceDoesNotDropReviews()
    {
        WebReviewQuery.Sql.Should().Contain("OUTER APPLY");
        WebReviewQuery.Sql.Should().Contain("t.TipoFuente = 'Web'");
    }

    [Fact]
    public void Sql_OrdersByPrimaryKeyForDeterministicRuns()
    {
        WebReviewQuery.Sql.Should().Contain("ORDER BY r.IdReview");
    }

    [Fact]
    public void Ordinals_MatchTheSelectOrder()
    {
        // SequentialAccess requires columns to be read in SELECT order, so these constants
        // and the SELECT must agree.
        var selectList = WebReviewQuery.Sql[..WebReviewQuery.Sql.IndexOf("FROM", StringComparison.Ordinal)];
        var columns = new[] { "r.IdReview", "r.IdCliente", "r.IdProducto", "r.Rating", "c.Comentarios", "fd.FechaCarga" };

        var positions = columns.Select(c => selectList.IndexOf(c, StringComparison.Ordinal)).ToList();

        positions.Should().NotContain(-1);
        positions.Should().BeInAscendingOrder();

        WebReviewQuery.IdReview.Should().Be(0);
        WebReviewQuery.FechaCarga.Should().Be(5);
    }
}
