create table public.t_kv
(
  id           uuid primary key,
  "group"      TEXT                     not null,
  "key"        TEXT                     not null,
  "value"      TEXT                     not null,
  description  TEXT,
  created_by   TEXT                     not null,
  created_date timestamp with time zone not null,
  updated_by   TEXT,
  updated_date timestamp with time zone,
  unique ("group", "key")
);
