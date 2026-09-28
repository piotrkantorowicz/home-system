-- Admin retry creates a new delivery linked to the failed one, which stays as history (status 'Retried').
ALTER TABLE notification_deliveries
    ADD COLUMN retry_of uuid NULL REFERENCES notification_deliveries(id) ON DELETE SET NULL;
