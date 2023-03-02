select *
from projects
left join scans on scans.ProjectId = projects.ProjectId;

select * 
from Literals l
join LiteralLocations ll on l.LiteralId = ll.LiteralId
join SourceFiles sf on sf.SourceFileId = ll.SourceFileId
where value like '%1';

