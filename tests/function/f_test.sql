create function f_test(@param varchar(100))
returns varchar(100)
as
begin

  declare @ret varchar(100);

  set @ret = 'Test: ' + @param;

  return @ret;

end
