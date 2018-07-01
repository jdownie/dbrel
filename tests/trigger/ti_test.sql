create trigger ti_test on test for insert
as
begin

  update t
  set name = 'T2: ' + t.name
  from test t
  ,    inserted i
  where t.id = i.id;

end
