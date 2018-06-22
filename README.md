# dbrel
Database Patch Release Tool

I am constantly working on a web application and it's accompanying database. That database requires constant changes to it's schema and routines. I need my workflow to be pretty responsive; I want to be able to make a change on my local development environment, and then either deploy those changes to UAT or Production quickly.

One half of that release procedure is concerned with the web pages - which is the easy part. The other half is the database which is a bit trickier.

I have been working on my own solution to this problem for a while now. Actually, I would say that I have solved it, but I seem to be perpetually improving it. By publishing this effort publicly, perhaps the end result can benefit from open source practices.

## Configuration (`~/.config/dbrel.json`)

This example configuration demonstrates two projects; `project1` and `project2`. `project1` is a pretty typical dev/uat/prd development cycle, but `project2` is a one site environment. Both projects have only one code-base each, and it's `dbrel`'s job to "project" the code in the specified folder into the specified database.

`targets` is a list of different destinations. `project1` calls it's three `dkr`, `uat` and `prd`. The signifigance of these three environments is arbitrariy. There is nothing here indicating that `prd` is more important than `uat` or `dkr`. They are, for our purposes, just targets for us to transmit our code to.

This configuration example really just maps code folders to database environments. `project1` has only one folder, but three targets. `project2` on the other hand has one folder and only one target. The contents of the folder nominated in the `path` setting is very important to the way that `dbrel` works.

```javascript
{ 'project1':
    { 'path': '/home/jdoe/Development/repo1/project1/Database'
    , 'patch_table_name': '_patch'
    , 'targets':
        [ { 'name': 'dkr'
          , 'connectionString': 'Server=localhost;Database=prj1db;User Id=sa;Password=secret;"'
          }
        , { 'name': 'uat'
          , 'connectionString': 'Server=uat.mybiz.com;Database=prj1db;User Id=deploy;Password=secret;"'
          }
        ]
        , { 'name': 'prd'
          , 'connectionString': 'Server=prd.mybiz.com;Database=prj1db;User Id=deploy;Password=secret;"'
          }
        ]
    }
, 'project2':
    { 'path': '/home/jdoe/Development/repo2/project2/DB'
    , 'patch_table_name': '_patch'
    , 'targets':
        [ { 'name': 'live'
          , 'connectionString': 'Server=www.mysite.com;Database=myapp;User Id=sa;Password=secret;"'
          }
        ]
    }
}
```

## Code Folders

`dbrel` is charged with the responsibility of deploying code from a nominated folder to a range of databases. There are two basic kinds of scripts that `dbrel` will be deploying...

* Schema Changes
* Procedure Changes

Procedure changes can be further divided into...

* Views
* Triggers
* Procedures
* Functions

Schema Changes and Procedure Changes are different because procedure changes can easily be dropped and re-run into the database. Schema changes on the other hand are a bit more delicate.

| Label | Code |
| --- | --- |
| A | `create table a ( int id not null )` |
| B | `alter table a add name varchar(100) not null` |
| C | `drop table a` |
| D | `create table a ( int id not null, name varchar(255) not null, address varchar(255) not null )` |
| E | `alter table a drop column address` |

Running these into a database in `ABCDE` order would give you...

```sql
create table a
( id int not null
, name varchar(255) not null
)
```

...but running them in in `DECAB` would give you...

```sql
create table a
( id int not null
, name varchar(100) not null
)
```

...and almost any other combination would fail with either a "table not found" or "column not found" error.

An example like this might look silly, but indecision like this in the development process is (in my experience) common. Trying to bear in mind the deployment process while contending with application design issues is unnecissarily distracting. Keeping the deployment procedure easily reproducable is precicely what `dbrel` is concerned with.

To achieve this `dbrel` creates a table for itself in each of the target databases. The configuration file lets you nominate what you want these tables called for each project. The table is a simple list of integers like this...

```sql
create table _patch ( id int not null )
```

`dbrel` is responsible for creating and populating this table.

So, having said all of that, `dbrel` expects the following folder structure...

* schema
* procedure
* function
* view
* trigger

Each of those folder will contain a collection of `.sql` files.

## Syntax

```bash
dbrel
```

## To Do List

### Dependencies

There is the tricky matter of dependencies. It's easy enough to get tangled up in "proc a depends on view b depends on function c". That's a whole mess I'm not ready to think about yet, but it's a potential problem in the future.