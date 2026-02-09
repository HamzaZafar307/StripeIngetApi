# Walkthrough - Stripe Ingestion Service

## Overview
This document outlines the implementation of the **Stripe Ingestion Service**, a robust API designed to process webhook events, maintain subscription state, and produce accurate Monthly Recurring Revenue (MRR) reports.

## Implementation Details

### Core Components
- **StripeIngest.Api**: .NET 9 Web API project.
- **Database**: SQL Server (participating via Docker).
- **ORM**: Entity Framework Core 9.0.

### Key Logic
- **Event Processing**: Idempotent handling of `customer.subscription.*` and `invoice.paid` events.
- **State Management**: Updates `CurrentSubscriptions` table and logs every change to `SubscriptionHistory`.
- **MRR Calculation**: Computes MRR dynamically based on `quantity * unit_amount`.

---

## Verification

### Automated Tests
Integration tests cover the following scenarios:
- **New Subscriptions**: Correctly creates state and history with "new" change type.
- **Upgrades**: Detects MRR increase and logs "upgrade".
- **Renewals**: Logs "renewal" event with no MRR change via `invoice.paid`.
- **Reporting**: SQL Views for efficient MRR calculation.
- **Idempotency**: Duplicate events are ignored.

Run tests using:
```bash
dotnet test StripeIngest.Tests
```
**Status**: Passed.



## Step-by-Step Testing Guide

Use **Swagger UI** (`http://localhost:5186/swagger`) to send these payloads to **`POST /api/Webhook`**.

### 1. New Subscription (New)
Creates a subscription with $100 MRR.

**Payload:**
```json
{
  "id": "evt_lifecycle_1",
  "type": "customer.subscription.created",
  "created": 1704067200,
  "data": {
    "object": {
      "id": "sub_test_1",
      "customer": "hamza",
      "status": "active",
      "items": { "data": [ { "quantity": 1, "plan": { "id": "price_100", "product": "prod_1", "amount": 10000, "currency": "usd", "interval": "month" } } ] }
    }
  }
}
```

### 2. Renewal (No Change)
Simulates a monthly payment. Logs a "renewal" event but **does not change MRR**.

**Payload:**
```json
{
  "id": "evt_lifecycle_2",
  "type": "subscription.updated",
  "created": 1706745600,
  "data": {
    "object": {
      "subscription": "sub_test_1",
      "customer": "hamza",
      "amount_paid": 10000,
      "status": "paid"
    }
  }
}
```

### 3. Upgrade (Expansion)
Increases amount to $200. Logs "upgrade" and +$100 MRR Expansion.

**Payload:**
```json
{
  "id": "evt_lifecycle_3",
  "type": "customer.subscription.updated",
  "created": 1709251200,
  "data": {
    "object": {
      "id": "sub_test_1",
      "customer": "hamza",
      "status": "active",
      "items": { "data": [ { "quantity": 1, "plan": { "id": "price_200", "product": "prod_2", "amount": 20000, "currency": "usd", "interval": "month" } } ] }
    }
  }
}
```

### 4. Downgrade (Contraction)
Decreases amount back to $100. Logs "downgrade" and -$100 MRR Contraction.

**Payload:**
```json
{
  "id": "evt_lifecycle_4",
  "type": "customer.subscription.updated",
  "created": 1711929600,
  "data": {
    "object": {
      "id": "sub_test_1",
      "customer": "hamza",
      "status": "active",
      "items": { "data": [ { "quantity": 1, "plan": { "id": "price_100", "product": "prod_1", "amount": 10000, "currency": "usd", "interval": "month" } } ] }
    }
  }
}
```

### 5. Cancellation (Churn)
Cancels the subscription. Logs "churn" and -$100 MRR (MRR becomes 0).

**Payload:**
```json
{
  "id": "evt_lifecycle_5",
  "type": "customer.subscription.deleted",
  "created": 1714521600,
  "data": {
    "object": {
      "id": "sub_test_1",
      "customer": "hamza",
      "status": "canceled",
      "items": { "data": [ { "quantity": 1, "plan": { "id": "price_100", "product": "prod_1", "amount": 10000, "currency": "usd", "interval": "month" } } ] }
    }
  }
}
```

### Verification
After sending these, check the history:
**`GET /api/Reports/customer/hamza/history`**

---

## Setup & Run

### Quick Start (Docker)
1. **Run Application**: `docker-compose up --build` (Auto-migrates DB)
2. **Access Swagger**: `http://localhost:5185/swagger`
3. **Webhook Endpoint**: `http://localhost:5185/api/webhook`
4. **Get All Events**: `GET http://localhost:5185/api/events`
5. **Get Yearly MRR**: `GET http://localhost:5185/api/reports/mrr/yearly`
6. **Get Customer History**: `GET http://localhost:5185/api/reports/customer/{customerId}/history`

### Multi-Item Subscriptions
The system supports subscriptions with multiple items (e.g., creating a subscription with 2 products). The MRR is calculated as the sum of all items (`quantity * amount`).

