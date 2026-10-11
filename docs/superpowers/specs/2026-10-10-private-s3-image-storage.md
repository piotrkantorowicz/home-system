# Private S3 image storage — issue #564

Source: https://github.com/piotrkantorowicz/home-system/issues/564
Parent: https://github.com/piotrkantorowicz/home-system/issues/563
Captured: 2026-10-10. Branch: `feat/564-private-s3-image-storage`, base `main`.

## Goal and scope

Establish private, module-owned S3-compatible storage before recipe image endpoints are added. Implement one storage port and one adapter for put/read/delete, cancellation, streaming reads, opaque object keys, and explicit failures. Add an opt-in local/CI service, persistent development storage, private bucket setup, credential-free examples, and production setup/recovery documentation. Existing startup must work without image storage configuration; attempted operations must report storage unavailable.

Contract checks must use a real S3-compatible test service and cover successful operations, missing objects, idempotent deletion, denied access, unavailable service, isolation, and cleanup.

## Acceptance criteria

- Put/read/delete succeeds against private storage; unauthenticated reads fail.
- Endpoint, region, bucket, and addressing style are configurable without SDK types in Domain/Application contracts.
- Credentials never enter frontend bundles, committed configuration, request URLs, or logs. Production guidance covers TLS and least privilege.
- Local object data survives service restart; CI fixtures isolate and clean their objects.
- Documentation covers coordinated database/object backup and restore, retention tradeoffs, and a production-provider compatibility smoke check.

## Boundaries

Recipe metadata, image decoding, normalization, concurrency, pending uploads, cleanup workers, HTTP endpoints, and authorization are #565. Detail UI is #566; thumbnails are #567; real-stack lifecycle/access coverage is #568. Production provider selection/purchase, cloud provisioning, direct browser uploads, public buckets, and CDN are excluded.

The parent requires opaque, immutable keys for each image revision and durable pending records before object writes. This slice accepts keys chosen by that future lifecycle; it must not generate keys after a write or expose provider URLs. Application operations return readable streams, never entire downloaded byte arrays. Missing object is distinct from missing bucket, denied access, and service outage.

No existing HTTP image operation exists in this slice. Its unavailable behavior is an application-level storage exception. #565 must map that condition to a safe HTTP 503 in the existing exception pipeline before exposing image routes. This is an explicit dependency, not a claim that #564 adds HTTP endpoints.
