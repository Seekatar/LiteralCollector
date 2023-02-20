select *
from source_file sf
join literal_location ll on sf.source_file_id = ll.source_file_id
join literal l on ll.literal_id = l.literal_id

