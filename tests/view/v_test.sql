create view v_test
as

  select id, name, 'View: ' + name as view_modified
  from test;

