with test as (
select l.LiteralId, sf.scanId, Count(*) as Count
from LiteralLocations ll
join Literals l on l.LiteralId = ll.LiteralId
join SourceFiles sf on sf.SourceFileId = ll.SourceFileId
group by l.LiteralId, sf.scanId
having count > 1
)
select l.Value, ll.LineStart, ll.ColumnStart, sf.Path, sf.FileName
from test t
join LiteralLocations ll on t.LiteralId = ll.LiteralId
join Literals l on l.LiteralId = ll.LiteralId
join SourceFiles sf on sf.SourceFileId = ll.SourceFileId
