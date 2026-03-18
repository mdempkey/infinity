CREATE TABLE "parks" (
  "id" varchar(20) PRIMARY KEY,
  "name" varchar(255) NOT NULL,
  "resort" varchar(255),
  "city" varchar(100),
  "country" varchar(100),
  "lat" decimal(9,6),
  "lng" decimal(9,6)
);

CREATE TABLE "categories" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "name" varchar(50) UNIQUE NOT NULL,
  "description" text
);

CREATE TABLE "attraction_categories" (
  "attraction_id" uuid NOT NULL,
  "category_id" uuid NOT NULL,
  PRIMARY KEY ("attraction_id", "category_id")
);

CREATE TABLE "attractions" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "park_id" varchar(20) NOT NULL,
  "name" varchar(255) NOT NULL,
  "description" text,
  "lat" decimal(9,6),
  "lng" decimal(9,6),
  "image_urls" jsonb,
  "tags" jsonb,
  "avg_rating" decimal(3,2) DEFAULT 0,
  "review_count" integer DEFAULT 0,
  "created_at" timestamp DEFAULT (now())
);

CREATE TABLE "users" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "username" varchar(50) UNIQUE NOT NULL,
  "email" varchar(255) UNIQUE NOT NULL,
  "password" varchar(255) NOT NULL,
  "created_at" timestamp DEFAULT (now())
);

CREATE TABLE "reviews" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "attraction_id" uuid NOT NULL,
  "user_id" uuid NOT NULL,
  "rating" smallint NOT NULL,
  "body" text,
  "visit_date" date,
  "created_at" timestamp DEFAULT (now()),
  "updated_at" timestamp
);

CREATE INDEX "idx_attractions_park_id" ON "attractions" ("park_id");

CREATE UNIQUE INDEX "one_review_per_user_per_attraction" ON "reviews" ("user_id", "attraction_id");

CREATE INDEX "idx_reviews_attraction_id" ON "reviews" ("attraction_id");

COMMENT ON COLUMN "parks"."id" IS 'e.g. park_gge_dla';

COMMENT ON COLUMN "categories"."name" IS 'e.g. ride, food, shop, show';

COMMENT ON COLUMN "attractions"."image_urls" IS 'Array of image URLs';

COMMENT ON COLUMN "attractions"."tags" IS 'Array of tag strings';

COMMENT ON COLUMN "attractions"."avg_rating" IS 'Cached, updated via trigger on review write';

COMMENT ON COLUMN "attractions"."review_count" IS 'Cached, updated via trigger';

COMMENT ON COLUMN "users"."password" IS 'bcrypt';

COMMENT ON COLUMN "reviews"."rating" IS '1–5';

COMMENT ON COLUMN "reviews"."body" IS 'Min 20 chars enforced in app';

ALTER TABLE "attractions" ADD FOREIGN KEY ("park_id") REFERENCES "parks" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "reviews" ADD FOREIGN KEY ("attraction_id") REFERENCES "attractions" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "reviews" ADD FOREIGN KEY ("user_id") REFERENCES "users" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attraction_categories" ADD FOREIGN KEY ("attraction_id") REFERENCES "attractions" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attraction_categories" ADD FOREIGN KEY ("category_id") REFERENCES "categories" ("id") DEFERRABLE INITIALLY IMMEDIATE;
