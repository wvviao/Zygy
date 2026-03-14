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
  enabled      boolean default true,
  unique ("group", "key")
);

CREATE TABLE public.t_files
(
  id           UUID PRIMARY KEY,
  filename     text NOT NULL,
  mime_type    text NOT NULL,
  etag         text not null,
  created_date TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
