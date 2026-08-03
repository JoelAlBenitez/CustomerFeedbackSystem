using System.Runtime.CompilerServices;

// SqlConnection is sealed and cannot be substituted by a test double, so the parts of the
// database extractor that ARE isolable — the row-to-entity projection — are internal and
// exposed here rather than left untested (doc 15 §5).
[assembly: InternalsVisibleTo("CustomerFeedbackSystem.OLAP.Tests")]
