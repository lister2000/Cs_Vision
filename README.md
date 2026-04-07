# Cs_Vision

A C# computer vision project.

---

## How to Change Repository Visibility from Private to Public

Follow these step-by-step instructions to change `lister2000/Cs_Vision` from **Private** to **Public** on GitHub. You must be the repository **Owner** or have **Admin** access to do this.

---

### ⚠️ Before You Begin — Security Checklist

Changing a repository to public exposes **all files and all git history** to the internet. Before proceeding, check every item below:

1. **Scan for secrets in files** — Remove any API keys, passwords, tokens, private keys, or credentials from all files (e.g. `.env`, `appsettings.json`, `config.json`, `*.pem`, `id_rsa`).
2. **Scan git history** — Even if you delete a secret file today, it may still exist in older commits. Use a tool like [git-secrets](https://github.com/awslabs/git-secrets) or [truffleHog](https://github.com/trufflesecurity/trufflehog) to scan the full history.
3. **Check GitHub Actions workflows** — If `.github/workflows/` exists, ensure no secrets are printed in logs or hardcoded in YAML files.
4. **Review all branches** — Each branch is also made public. Check any feature branches for sensitive data.

> **Important:** Once a repository is made public, any secrets that were exposed (even briefly) should be considered compromised. Rotate those credentials immediately, regardless of whether you make the repo private again.

---

### Step-by-Step: Change to Public

**Step 1 — Open the repository**

Go to: `https://github.com/lister2000/Cs_Vision`

**Step 2 — Open Settings**

Click the **Settings** tab in the top navigation bar of the repository page (it has a gear icon ⚙️).

> If you do not see **Settings**, you do not have Admin or Owner access to this repository.

**Step 3 — Scroll to Danger Zone**

Scroll all the way to the **bottom** of the Settings page. You will see a red section labelled **Danger Zone**.

**Step 4 — Click "Change visibility"**

Inside the Danger Zone, find the row labelled **Change repository visibility**. Click the **Change visibility** button on the right side of that row.

**Step 5 — Select "Make public"**

A dialog box will appear. Select the option **Make public**.

GitHub will show a warning listing what this means:
- This repository and all forks will be publicly visible.
- Commit history and all branches will be public.
- Any private GitHub Actions runs may be visible.

Read the warning, then click **I want to make this repository public**.

**Step 6 — Confirm by typing the repository name**

GitHub requires you to confirm by typing the full repository name in the text box:

```
lister2000/Cs_Vision
```

Type this exactly as shown, then click **I understand, change repository visibility**.

**Step 7 — Re-authenticate if prompted**

GitHub may ask you to confirm your password or complete two-factor authentication (2FA). Complete the authentication step to finalize the change.

---

### Verifying the Change

After the change, confirm the repository is now public:

1. **Check the label on the repository page** — Next to the repository name at the top, you should see a **Public** badge (previously it showed **Private**).
2. **Use an incognito/private browser window** — Open `https://github.com/lister2000/Cs_Vision` without being logged in. If you can view the code without signing in, the repository is public.
3. **Check via GitHub API** — Run the following command in a terminal (no authentication required for public repos):
   ```bash
   curl https://api.github.com/repos/lister2000/Cs_Vision | grep '"private"'
   ```
   A public repository will return `"private": false`.

---

### What to Do If the Option Is Restricted

If you cannot find the **Change visibility** option, or if clicking it shows an error, the likely causes and solutions are:

| Situation | Solution |
|---|---|
| You are not the Owner or Admin of the repository | Ask the repository owner to make the change, or request that they grant you Admin access under **Settings → Collaborators and teams**. |
| The repository belongs to a GitHub **organization** and the organization policy restricts making repos public | Contact an **organization owner**. They can change the policy under **Organization Settings → Member privileges → Repository visibility change**. |
| You are on a GitHub **Enterprise** plan with enforced repository visibility policies | Contact your GitHub Enterprise administrator to request an exception or change the organization-level policy. |
| The account has a pending email verification | Complete email verification in your GitHub account settings, then try again. |

---

### Reverting to Private (if needed)

If you change your mind after making the repository public, you can follow the same steps above and select **Make private** instead. However, note:

- Any data that was public (even for a short time) may have been crawled, cached, or forked by others.
- Cached versions may remain accessible via search engines or tools like the Wayback Machine.
- Any secrets exposed during the public window must still be rotated.
