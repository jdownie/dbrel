# dbrel
Database Patch Release Tool

I am constantly working on a web application and any accompanying databases. That database requires constant changes to it's schema and routines. I need my workflow to be pretty responsive; I want to be able to make a change on my local development environment, and then either deploy those changes to UAT or Production quickly.

One half of that release procedure is concerned with the web pages - which is the easy part. The other half is the database which is a bit trickier.

I have been working on my own solution to this problem for a while now. Actually, I would say that I have solved it, but I seem to be perpetually improving it. By publishing this effort publicly, perhaps the end result can benefit from open source practices.

## Configuration (`.dbrel`)

Example 1 below is a pretty typical dev/uat/prd development cycle, but Example 2 is a one site environment. In both cases, the `.dbrel` configuration file is hosted in the root of the `dbrel` folder structure (which is described in more detail below).

Example 1 calls it's three targets `dkr`, `uat` and `prd`. The significance of these three environments is arbitrary. There is nothing here indicating that `prd` is more important than `uat` or `dkr`. They are, for our purposes, just targets for us to transmit our code to. The fact that `dkr` is first is however significant, and makes `dkr` the default target if no specific target is nominated.

### Example 1
```javascript
{ "dkr":
    { "connectionString": "Server=localhost;Database=prj1db;User Id=sa;Password=secret;"
    , "cfg": "
exec pu_setting('attachments_dir', '/tmp/attachments');
exec pu_setting('admin_email', 'me@home.com');
"
    }
, "uat":
    { "connectionString": "Server=uat.mybiz.com;Database=prj1db;User Id=deploy;Password=secret;"
    , "cfg": "
exec pu_setting('attachments_dir', '/tmp/attachments');
exec pu_setting('admin_email', 'me@home.com');
"
    , "ignore_patterns": ".*apple.*\\.sql"
    }
, "prd":
    { "connectionString": "Server=prd.mybiz.com;Database=prj1db;User Id=deploy;Password=secret;"
    , "patch_table": "_patch"
    }
}
```

Example 1 also demonstrates another `cfg` feature. Because I am often refreshing my Docker and UAT databases from production backups, it's easy to accidentally overwrite certain development and UAT specific settings. The `cfg` property is a script that can be run at any time to remind a target environment what sets it apart from production. In the example provided, a stored procedure `pu_setting` updates a `setting` table with values that govern the behaviour of the application in any of the three environments.

The "uat" target also demonstrates the "ignore_patterns" feature. If you're (like me) occasionally guilty of running in somebody else's code when they'd prefer you left it alone you might find this helpful.

### Example 2
```javascript
{ "live":
    { "connectionString": "Server=www.mysite.com;Database=myapp;User Id=sa;Password=secret;"
    }
}
```

## Code Folders

`dbrel` is responsible for deploying code from a nominated folder to a range of databases. There are two basic kinds of scripts that `dbrel` will be deploying...

* Schema Changes
* Procedure Changes

Procedure changes can be further divided into...

* Views
* Triggers
* Procedures
* Functions

Schema Changes and Procedure Changes are different because procedure changes can easily be dropped and re-run into the database. Schema changes on the other hand are a bit more delicate.

Consider the following schema changes, labelled A through to E...

| Label | Code |
| --- | --- |
| A | `create table a ( int id not null )` |
| B | `alter table a add name varchar(100) not null` |
| C | `drop table a` |
| D | `create table a ( int id not null, name varchar(255) not null, address varchar(255) not null )` |
| E | `alter table a drop column address` |

Running these into a database in `ABCDE` order would result in...

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

An example like this might look silly, but indecision like this in the development process is (in my experience) common. Trying to bear in mind the deployment process while also working on application design issues is unnecessarily distracting. Keeping the deployment procedure easily reproducible is precisely what `dbrel` is concerned with.

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
* index

Each of those folder will contain a collection of `.sql` files.

It is important that the files in `procedure`, `function`, `view`, and `trigger` are named specifically after the object that they will create. This is important because `dbrel` will want to use that file name to fashion a `drop` statement. For example, `procedure/p_list.sql` will contain a script that begins with `create procedure p_list`. `dbrel` will want to issue a `drop procedure p_list` before attempting to run-in the contents of `procedure/p_list.sql` against the target.

Schema change file names are a different beast. they are numbered sequentially; `1.sql` followed by `2.sql`, etc. Take for example a schema folder containing...

* `1.sql`
* `2.sql`
* `3.sql`
* `4.sql`
* `5.sql`
* `6.sql`

Now consider the three target databases from Example 1 above. Each of those databases contain a `dbrel` "owned" table called `_patch` by default.

`dkr`'s `_patch` table contains...

| id |
| --- |
| 1 |
| 2 |
| 3 |
| 4 |
| 5 |
| 6 |

...which accounts for each of the six `.sql` files in the `schema` folder. `uat`'s `_patch` table on the other hand contains...

| id |
| --- |
| 1 |
| 2 |
| 3 |

...and so running `dbrel -t uat -s queue` will list...

```bash
4
5
6
```

...and running `dbrel -t uat -s apply` will deploy them to `uat`...

```bash
$ dbrel -t uat -s apply
Patch 4 successfully deployed to uat.
$ dbrel -t uat -s apply
Patch 5 successfully deployed to uat.
$ dbrel -t uat -s apply
Patch 6 successfully deployed to uat.
```

Because directory listings are typically sorted alphabetically, `dbrel` is a little forgiving of the naming convention in the `schema` folder. Any leading zeros are trimmed from the filenames bu `dbrel`, so `00001.sql`, `001.sql` and `1.sql` are all the same thing.

## Example Usages

### Schema Changes

Consider the following

| Command | Description |
| --- | --- |
| `dbrel -i .` | Initialise/verify the folder structure of the nominated folder |
| `dbrel -i -t uat` | Initialise the patch table in the `uat` target |
| `dbrel ./procedure/p_list.sql` | Drop and re-run the `p_list` procedure |
| `dbrel -t uat ./procedure/p_list.sql` | Drop and re-run the `p_list` procedure in the `uat` target |
| `dbrel -t uat -s queue` | List all of the schema changes that have not yet been applied to the `uat` target. |
| `dbrel -t uat -s apply` | Apply the next available 

```bash
dbrel -i
```

## To Do List

### Dependencies

There is the tricky matter of dependencies. It's easy enough to get tangled up in "proc a depends on view b depends on function c". That's a whole mess I'm not ready to think about yet, but it's a potential problem in the future.
