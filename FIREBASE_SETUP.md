# Firebase Setup for DevLauncher

All team preset data is stored in **your Firebase project**.
Users only need a workspace code — no Firebase account required on their end.

---

## Step 1 — Create a Firebase Project

1. Go to [https://console.firebase.google.com](https://console.firebase.google.com)
2. Click **Add project**
3. Name it `devlauncher` (or anything you want)
4. Disable Google Analytics (not needed) → **Create project**

---

## Step 2 — Enable Realtime Database

1. In the left sidebar → **Build** → **Realtime Database**
2. Click **Create Database**
3. Choose your region (pick closest to your team)
4. Start in **Test mode** for now (we'll lock it down below)
5. Click **Enable**

Your database URL will look like:
```
https://devlauncher-xxxxx-default-rtdb.firebaseio.com
```
**Copy this URL** — you'll need it in Step 4.

---

## Step 3 — Get Your Web API Key

1. In Firebase Console → **Project Settings** (gear icon top left)
2. Scroll to **Your apps** section
3. Click **Add app** → choose **Web** (`</>`)
4. Register the app (name doesn't matter)
5. Copy the `apiKey` value from the config shown:

```javascript
const firebaseConfig = {
  apiKey: "AIzaSy...",          // ← copy this
  databaseURL: "https://..."    // ← and this
};
```

---

## Step 4 — Add Your Config to FirebaseService.cs

Open `Services/FirebaseService.cs` and replace the placeholder values:

```csharp
// ── YOUR FIREBASE CONFIG ──────────────────────────────────────
private const string DatabaseUrl = "https://YOUR-PROJECT-default-rtdb.firebaseio.com";
private const string ApiKey      = "YOUR-WEB-API-KEY";
// ─────────────────────────────────────────────────────────────
```

Replace with your actual values:

```csharp
private const string DatabaseUrl = "https://devlauncher-abc12-default-rtdb.firebaseio.com";
private const string ApiKey      = "AIzaSyBxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
```

---

## Step 5 — Set Database Security Rules

In Firebase Console → **Realtime Database** → **Rules** tab,
paste these rules and click **Publish**:

```json
{
  "rules": {
    "workspaces": {
      "$code": {
        ".read": true,
        ".write": true
      }
    },
    "ping": {
      ".read": true,
      ".write": false
    }
  }
}
```

This means:
- Anyone with a workspace **code** can read/write that workspace's presets
- The `ping` node is read-only (used for connection testing)
- No one can list all workspaces (security through obscurity on codes)

---

## Step 6 — Create Your First Workspace

After building and running DevLauncher:

1. Go to the **Team** tab
2. Enter any code you want (e.g. `MYTEAM-2024`)
3. Enter your display name
4. Click **Connect to Workspace**

If the workspace doesn't exist yet, it will be **auto-created** on first connect.

Share the workspace code `MYTEAM-2024` with your teammates.
They enter the same code → everyone syncs instantly.

---

## Database Structure

Your Firebase Realtime Database will look like this:

```
devlauncher-xxxxx-default-rtdb
└── workspaces/
    └── MYTEAM-2024/
        ├── name: "My Team"
        ├── createdAt: "2024-01-01T00:00:00Z"
        └── presets/
            ├── abc123def/
            │   ├── name: "Node.js Full Stack"
            │   ├── icon: "🚀"
            │   ├── category: "Dev"
            │   ├── lastEditedBy: "John"
            │   ├── lastEditedAt: "2024-01-15T09:30:00Z"
            │   └── commands/
            │       ├── 0: { command: "npm install", ... }
            │       └── 1: { command: "npm run dev", ... }
            └── xyz789ghi/
                └── ...
```

---

## Firebase Free Tier Limits (Spark Plan)

| Resource | Free limit | Typical DevLauncher usage |
|----------|-----------|--------------------------|
| Simultaneous connections | 100 | 100 team members |
| Storage | 1 GB | ~50,000 presets |
| Download / month | 10 GB | Millions of syncs |
| Upload / month | Unlimited | ✓ |

**The free tier is more than enough for most teams.**

---

## Monitoring Usage

Firebase Console → **Realtime Database** → **Usage** tab
shows read/write counts, active connections, and storage used in real time.

---

## Multiple Teams / Workspaces

You can have unlimited workspaces in one Firebase project.
Just give each team a different code:

| Team | Code |
|------|------|
| Frontend | `FRONTEND-2024` |
| Backend | `BACKEND-2024` |
| DevOps | `DEVOPS-OPS` |
| Client A | `ACME-CORP` |

Each is completely isolated in the database.
