# AI Capabilities: Sentry + Slack Integration Guide
> **Stack:** .NET · GitHub · GitHub Copilot · Azure DevOps  
> **Last updated:** July 2026

---

## Table of Contents

1. [Overview](#overview)
2. [Sentry AI — Seer](#sentry-ai--seer)
   - [Autofix: Root Cause → PR in Minutes](#autofix-root-cause--pr-in-minutes)
   - [Seer Agent (Ask Anything)](#seer-agent-ask-anything)
   - [AI Code Review](#ai-code-review)
3. [Sentry MCP Server](#sentry-mcp-server)
4. [Sentry + Slack Integration](#sentry--slack-integration)
   - [Issue Alerts](#issue-alerts)
   - [Fix with Seer from Slack](#fix-with-seer-from-slack)
   - [Seer Agent in Slack](#seer-agent-in-slack)
5. [Sentry + GitHub Copilot (Automated PR Creation)](#sentry--github-copilot-automated-pr-creation)
6. [Sentry + .NET Setup](#sentry--net-setup)
7. [Sentry + Azure DevOps — Limitations & Workarounds](#sentry--azure-devops--limitations--workarounds)
8. [Slack AI Features](#slack-ai-features)
9. [Pricing Summary](#pricing-summary)
10. [Quick-Start Checklist](#quick-start-checklist)

---

## Overview

| Tool | AI Role |
|------|---------|
| **Sentry (Seer)** | Debugging agent: auto-detects root cause, drafts fixes, opens PRs |
| **Sentry MCP** | Connects Sentry data directly to any LLM/coding agent via MCP protocol |
| **Slack + Sentry** | Trigger Seer fixes from alert threads; chat with Seer Agent in Slack |
| **GitHub Copilot + Sentry** | Seer hands off root-cause analysis → Copilot opens a PR automatically |
| **Slack AI** | Summaries, search, Slackbot agent, workflow automation |

---

## Sentry AI — Seer

Seer is Sentry's built-in AI debugging agent. It reads your telemetry (errors, traces, logs, profiles) and your codebase to **understand, diagnose, and fix** issues automatically.

### Autofix: Root Cause → PR in Minutes

Autofix is the core Seer workflow. It runs in three stages:

```
1. Root Cause Analysis (RCA)
      ↓  (reads stack trace, traces, logs, code)
2. Solution Identification
      ↓  (proposes fix plan; you can edit/approve)
3. Code Generation + PR
      ↓  (creates branch, commits changes, opens PR on GitHub/GitLab)
```

#### Triggering Autofix

| Trigger | How |
|---------|-----|
| **Manual** | Open any Sentry issue → click **Start Root Cause Analysis** |
| **Automatic** | Enable in project Seer settings; fires when issue has ≥10 events AND is ≤14 days old AND has high fixability score |
| **From Slack alert** | Click **Fix with Seer** button on any Sentry alert in Slack *(Early Adopter feature)* |

#### How Far It Goes (Configurable)

In **Settings → Seer → Projects**, you set the automation ceiling per project:

| Stop Point | What Seer Does Automatically |
|------------|------------------------------|
| **Root Cause only** | Identifies the cause; no code changes |
| **Plan** | RCA + solution plan; no PR |
| **PR Drafted** | Full RCA + plan + opens a PR ← most automated |

#### What Seer Reads

- Issue details, stack traces, event metadata  
- Distributed traces and span data  
- Structured logs (beta)  
- Source code from linked GitHub or GitLab repos  
- Performance profiles and metrics  
- Your feedback/instructions during the session  
- Sentry documentation  

#### Supported SCM Providers

> ⚠️ **Important for Azure DevOps users:** Seer Autofix and Code Review support **GitHub.com and GitLab.com cloud only**. Azure DevOps repositories are **not supported** for direct Seer PR creation. See the [Azure DevOps workarounds section](#sentry--azure-devops--limitations--workarounds).

---

### Seer Agent (Ask Anything)

Seer Agent is a conversational AI assistant with full access to all your Sentry telemetry — not just individual issues.

> Status: **Open Beta**

**Use it to:**
- Ask "why is latency high on `/api/checkout` this week?"
- Investigate cross-service incidents in plain language
- Walk through complex production problems step by step
- Share conversations with teammates or copy to other AI agents

**How to access:**
- Click **Ask Seer** on any page in Sentry
- Via Slack: @mention `@sentry` in any channel (see [Seer Agent in Slack](#seer-agent-in-slack))

> Note: Requires **Open Team Membership** to be enabled in org settings (Seer Agent makes org-wide data queries).

---

### AI Code Review

Seer reviews your PRs **before merge**, using real production error data to predict whether your changes will cause failures.

#### How It Works

1. Open a PR / mark it **Ready for Review** on GitHub or GitLab
2. Seer automatically scans the diff against known error patterns from your production data
3. Results appear as:
   - PR comments with specific findings
   - A **GitHub Status Check** (green ✅ = no issues, yellow ⚠️ = issues found, red ❌ = review error)
4. You can also manually trigger: comment `@sentry review` on any PR

#### GitHub Status Check States

| State | Meaning |
|-------|---------|
| ✅ Success | No errors predicted |
| ⚠️ Neutral | Potential errors found (see PR comments) |
| ❌ Error | Review failed (service issue / timeout) |
| Cancelled | Superseded by a newer commit |

> Recommendation: Add Code Review as an **optional** (not required) branch protection check to avoid blocking merges on service disruptions.

---

## Sentry MCP Server

The **Sentry MCP Server** exposes Sentry's full API to any LLM or coding agent that supports the Model Context Protocol (MCP). Think of it as a live data bridge between your AI coding assistant and your production Sentry data.

**MCP Endpoint:**
```
https://mcp.sentry.dev/mcp
```

### What You Can Do via MCP

| Capability | Example |
|------------|---------|
| **Fix bugs** | Paste a Sentry issue URL → LLM fetches details, runs Seer analysis, applies a code fix |
| **Instrument your app** | Ask the LLM to add proper Sentry SDK setup to your codebase |
| **Search issues** | "Find all NullReferenceException errors in the last 7 days" |
| **Analyze traces** | "What's causing the P95 latency spike in the checkout service?" |
| **Root cause + fix** | Full Autofix workflow triggered from inside your IDE |

### Installation by Tool

#### VS Code (GitHub Copilot)
```json
// .vscode/mcp.json or settings.json
{
  "mcp": {
    "servers": {
      "sentry": {
        "type": "http",
        "url": "https://mcp.sentry.dev/mcp"
      }
    }
  }
}
```

#### Claude Code
```bash
claude mcp add --transport http sentry https://mcp.sentry.dev/mcp
```

#### Cursor
Add to Cursor settings:
- Transport: `http`
- URL: `https://mcp.sentry.dev/mcp`

### Advanced MCP Options

**Path Constraints** — Scope the server to a specific org or project:
```
https://mcp.sentry.dev/mcp/:organization
https://mcp.sentry.dev/mcp/:organization/:project
```

**Agent Mode** — Instead of individual tools, expose a single `use_sentry` tool that handles natural language routing:
```
https://mcp.sentry.dev/mcp?agent=1
```

### MCP Workflow Example (GitHub Copilot in VS Code)

1. Open GitHub Copilot Chat in VS Code
2. Paste a Sentry issue URL into the chat
3. Copilot calls Sentry MCP → fetches issue details → calls Seer analysis
4. Copilot proposes a fix and applies it directly to your codebase
5. Tests run automatically to verify the fix

---

## Sentry + Slack Integration

### Setup

1. In Sentry: **Settings → Integrations → Slack → Add Workspace**
2. Authorize the Sentry Slack app in your workspace
3. Link your personal account: type `/sentry link` in any Slack channel
4. Link a team: `/sentry link team [org-slug]` in the desired channel

### Issue Alerts

After connecting Slack, you can route Sentry alerts to any channel:

- In an alert rule, set **Action = Notify via Slack**
- Choose workspace + channel
- Optionally add tags and custom text (e.g., `@oncall-team`)

**Alert capabilities by issue type:**

| Issue Type | Slack Actions Available |
|------------|------------------------|
| Error | Resolve · Archive · Assign · **Fix with Seer** |
| Metric | Chart of metric history + follow-up updates as threaded replies |
| All types | View in Sentry, link to issue details |

### Fix with Seer from Slack

> ⚠️ **Early Adopter feature** — Enable at: Settings → Organization → Early Adopter

When an issue alert fires in Slack:

1. The alert includes a **Fix with Seer** button
2. Click it → Seer starts Autofix immediately in the background
3. Results post to the Slack thread (root cause + fix plan)
4. You can approve and let Seer open a PR — all without leaving Slack

### Seer Agent in Slack

> Status: **Open Beta**

Use Seer Agent conversationally from inside Slack:

**Method 1 — @mention in a channel:**
```
@sentry why are we getting 500s on /api/orders since the last deploy?
```
Seer reads context from the thread + your Sentry data and replies inline.

**Method 2 — AI Assistant Panel (paid Slack plans):**
On Slack Pro, Business+, or Enterprise+ (with AI assistant apps enabled):
- Click the dropdown in the top-right of Slack
- Open Seer Agent as a persistent panel
- Chat privately without @mentioning every message
- New chats start with example prompts

**Multi-org setup:** If you have multiple Sentry orgs connected to one Slack workspace:
```
/sentry set org [organization_slug]
```

### Required Slack Bot Permissions

| Permission | Purpose |
|------------|---------|
| `assistant:write` | Seer acts as an AI assistant in Slack |
| `app_mentions:read` | Detect @sentry mentions |
| `chat:write` | Post alerts and Seer responses |
| `commands` | Enable `/sentry` slash commands |
| `channels:read` / `groups:read` | Validate alert channels |
| `channels:history` / `groups:history` | Read thread context for Seer Agent (optional) |
| `links:read` / `links:write` | Rich unfurling of sentry.io links |

---

## Sentry + GitHub Copilot (Automated PR Creation)

This integration creates a **fully automated pipeline**: production error → AI analysis → pull request opened automatically.

### Architecture

```
Production Error (Sentry)
        ↓
   Seer Autofix
   (Root Cause Analysis + Solution Plan)
        ↓
   Send to GitHub Copilot Agent
        ↓
   Copilot runs in GitHub Actions
        ↓
   Pull Request opened automatically
```

### Setup

1. **Enable GitHub Copilot Cloud Agent** in your GitHub organization settings
   - Requires GitHub Copilot Enterprise or appropriate plan
   - Consumes GitHub Actions minutes + Copilot premium requests

2. **Install GitHub Copilot integration in Sentry:**
   - Settings → Integrations → search **GitHub Copilot** → Install

3. **First-time Authorization:**
   - Open any Sentry issue → click **Start Root Cause Analysis**
   - After RCA completes → click **Set Up GitHub Copilot** in the Seer panel
   - Complete OAuth flow to link your GitHub account

### Using the Integration

1. In any Sentry issue, run Root Cause Analysis (Seer)
2. Once complete, open the dropdown at the code generation step
3. Select **Send to GitHub Copilot**
4. A link to the Copilot agent run and the resulting PR appears in Sentry UI
5. Track Copilot's progress from the link; review and merge the PR

### Comparison: Seer Direct PR vs. Copilot Agent PR

| Feature | Seer Direct PR | Copilot Agent PR |
|---------|----------------|-----------------|
| Creates PR | ✅ | ✅ |
| Uses Seer RCA | ✅ | ✅ (Seer analysis is passed in) |
| Code generation by | Seer | GitHub Copilot |
| Requires GitHub Actions | No | Yes |
| Can run without IDE open | Yes | Yes |
| Best for | Fast, targeted fixes | Complex code generation |

---

## Sentry + .NET Setup

### Installation

```bash
# Core SDK
dotnet add package Sentry

# ASP.NET Core (recommended)
dotnet add package Sentry.AspNetCore

# Optional: Profiling
dotnet add package Sentry.Profiling
```

### ASP.NET Core Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(options =>
{
    options.Dsn = "https://<key>@o<orgId>.ingest.sentry.io/<projectId>";
    
    // Enable tracing for Seer's root-cause analysis
    options.TracesSampleRate = 1.0;
    
    // Enable profiling (optional, improves Seer analysis)
    options.ProfilesSampleRate = 1.0;
    
    // Include user info for better context
    options.SendDefaultPii = true;
    
    // Tag releases so Seer can track which deploy introduced issues
    options.Release = "myapp@1.0.0";
    
    // Environment tagging
    options.Environment = builder.Environment.EnvironmentName;
});
```

### Supported .NET Frameworks & Integrations

| Framework/Library | NuGet Package |
|-------------------|--------------|
| ASP.NET Core | `Sentry.AspNetCore` |
| ASP.NET (classic) | `Sentry.AspNet` |
| Azure Functions (isolated) | `Sentry.AzureFunctions.Worker` |
| Entity Framework Core | `Sentry.EntityFramework` |
| Serilog | `Sentry.Serilog` |
| NLog | `Sentry.NLog` |
| log4net | `Sentry.Log4Net` |
| Microsoft.Extensions.Logging | `Sentry.Extensions.Logging` |
| MAUI | `Sentry.Maui` |
| Blazor WebAssembly | `Sentry.AspNetCore` (via JS SDK + .NET) |

### Serilog Example (common in .NET)

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Sentry(o =>
    {
        o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
        o.MinimumEventLevel = LogEventLevel.Error;
    })
    .CreateLogger();
```

### Best Practices for Seer

- **Enable tracing** (`TracesSampleRate > 0`) — Seer uses distributed traces for root cause analysis
- **Enable structured logging** — helps Seer correlate log events with errors
- **Set `Release`** — lets Seer identify which commit introduced an issue via suspect commits
- **Upload source maps** (for JavaScript frontends) — critical for minified code analysis
- **Add custom tags/context** to events for richer Seer analysis:

```csharp
SentrySdk.ConfigureScope(scope =>
{
    scope.SetTag("feature-flag", "checkout-v2");
    scope.SetExtra("order-id", orderId);
    scope.User = new SentryUser { Id = userId, Email = userEmail };
});
```

---

## Sentry + Azure DevOps — Limitations & Workarounds

> **Critical Note:** Seer Autofix and Code Review currently support **GitHub.com and GitLab.com cloud only**.  
> Azure DevOps repositories are **not directly supported** for Seer-generated PR creation.

### What Works with Azure DevOps

| Feature | Azure DevOps Support |
|---------|---------------------|
| Error monitoring (.NET SDK) | ✅ Full support |
| Slack alerts | ✅ Full support |
| Issue alerts & notifications | ✅ Full support |
| Seer Agent (ask questions) | ✅ Works (reads Sentry data, not your repo) |
| Seer Root Cause Analysis | ✅ Works (reads Sentry telemetry) |
| Seer Code Generation / PR | ❌ Not supported (no Azure Repos integration) |
| Seer Code Review on PRs | ❌ Not supported |
| Sentry MCP | ✅ Works (query Sentry data from any LLM) |

### Workarounds for Azure DevOps Teams

#### Option 1: Mirror critical repos to GitHub
- Keep Azure DevOps as the primary SCM for CI/CD pipelines
- Mirror select repositories to GitHub.com for Seer integration
- Seer PRs are created on GitHub mirror; manually applied to Azure Repos

#### Option 2: Use Sentry MCP + GitHub Copilot in VS Code
1. Connect Sentry MCP to VS Code GitHub Copilot Chat
2. When an alert fires, ask Copilot Chat to fix the issue using MCP data
3. Copilot reads Sentry context and generates the fix locally
4. You create the Azure DevOps PR manually or via CLI

```
# Example Copilot Chat prompt with Sentry MCP
"Fix this Sentry issue: https://yourorg.sentry.io/issues/12345678/"
```

#### Option 3: Seer RCA → Manual PR Flow
1. Use Seer in Sentry UI to get full root cause analysis + code fix plan
2. Implement the suggested fix locally (Seer shows exact diff)
3. Create PR in Azure DevOps with the fix

#### Option 4: Use Seer Agent + Slack for Investigation
- Even without repo integration, Seer Agent can analyze traces, logs, and errors
- Use Slack integration to trigger investigations from alert channels
- Seer Agent provides the fix code as a snippet you apply manually

---

## Slack AI Features

### AI Capabilities by Plan

| Feature | Free | Pro | Business+ | Enterprise+ |
|---------|------|-----|-----------|-------------|
| **AI conversation summaries** | Basic | ✅ | ✅ Advanced | ✅ Enterprise |
| **Thread/channel summaries** | ❌ | ✅ | ✅ | ✅ |
| **Huddles notes (AI)** | ❌ | ✅ | ✅ | ✅ |
| **AI assistant apps** (e.g., Sentry Seer Agent) | ❌ | ✅ | ✅ | ✅ |
| **Slackbot (personal AI agent)** | ❌ | ❌ | ✅ NEW | ✅ |
| **Daily recaps** | ❌ | ❌ | ✅ | ✅ |
| **File summaries** | ❌ | ❌ | ✅ | ✅ |
| **AI language translations** | ❌ | ❌ | ✅ | ✅ |
| **AI workflow generation** | ❌ | ❌ | ✅ | ✅ |
| **AI steps in Workflow Builder** | ❌ | ❌ | ✅ | ✅ |
| **AI message explanations** | ❌ | ❌ | ✅ | ✅ |
| **AI writing assistance (Canvas)** | ❌ | ❌ | ✅ | ✅ |
| **AI search** | ❌ | ❌ | ✅ | ✅ |
| **Enterprise search** | ❌ | ❌ | ❌ | ✅ |

> **For Sentry's Seer Agent as an AI Assistant in Slack:** requires **Pro plan or higher** (AI assistant apps feature).

### Key AI Features for Engineering Teams

**Slackbot (Business+ and above)**  
Your personal AI agent that knows your workspace. Useful for:
- Preparing for incident retrospectives
- Summarizing what happened in a channel during an outage
- Drafting postmortems
- Creating project briefs

**AI Workflow Generation**  
Describe a workflow in plain language → Slack builds it automatically. Great for:
- Auto-posting Sentry alerts to incident channels
- Escalation workflows when Seer triggers Autofix
- On-call rotation notifications

**AI Search (Business+ and above)**  
Searches across Slack messages, files, integrated tools (GitHub PRs, Sentry issues, etc.) using semantic search.

**Daily Recaps**  
Morning digest of what's important based on your priorities — surfaces unread critical alerts, incident updates, or PR review requests.

---

## Pricing Summary

### Sentry Plans

| Plan | Price | Users | Key Limits |
|------|-------|-------|-----------|
| **Developer** | Free | 1 | 5k errors, 5GB logs, 5M spans |
| **Team** | $26/mo (annual) | Unlimited | 50k errors, 5GB logs, 5M spans, 50 replays |
| **Business** | $80/mo (annual) | Unlimited | 50k errors + expanded features (anomaly detection, etc.) |
| **Enterprise** | Custom | Unlimited | Custom limits, SLAs, dedicated support |

> All paid plans include: 14-day free trial.  
> Pay-as-you-go (PAYG) available for overages.

### Seer AI Add-on Pricing

| Metric | Price |
|--------|-------|
| **Per active contributor / month** | **$40** |
| Active contributor definition | Any person who makes **2 or more PRs** in a Seer-enabled repo in a month |
| Seer-enabled repo | A repo connected to Sentry with at least one Seer feature enabled |
| Free trial | 14-day Seer trial available |
| Plan requirement | Team, Business, or Enterprise (not free Developer plan) |

**Cost Examples:**

| Team Size | Active Contributors / Month | Monthly Seer Cost |
|-----------|---------------------------|------------------|
| 5 developers | 5 | $200/mo |
| 10 developers | 8 | $320/mo |
| 20 developers | 15 | $600/mo |

> Contributors reset every month. GitHub bots (`[bot]` accounts) are excluded from counting.

### Sentry PAYG Data Rates (Team Plan)

| Data Category | Included | PAYG Rate |
|--------------|----------|-----------|
| Errors | 50k/mo | $0.00036/error (>50k-100k) |
| Logs | 5GB/mo | $0.50/GB |
| App Metrics | 5GB/mo | $0.50/GB |
| Spans (Tracing) | 5M/mo | $0.000002/span (>5M-100M) |
| Session Replays | 50/mo | $0.003/replay |
| Cron Monitors | 1 | $0.78/monitor |
| Uptime Monitors | 1 | $1.00/monitor |
| Continuous Profiling | PAYG only | $0.0315/hour |
| UI Profiling | PAYG only | $0.25/hour |

### Slack Plans

| Plan | Price | Key AI Features |
|------|-------|----------------|
| **Free** | $0 | Basic AI (summaries only), 90-day message history |
| **Pro** | $8.75/user/mo (monthly) | Thread summaries, AI assistant apps (Sentry Seer Agent access) |
| **Business+** | $18/user/mo (monthly) | Full AI suite: Slackbot, daily recaps, AI search, workflow AI, translations |
| **Enterprise+** | Contact sales | Enterprise-grade AI, enterprise search, enterprise compliance |

> Current promotional pricing: 50% off for 3 months on Pro and Business+.

---

## Quick-Start Checklist

### 🚀 For Teams Starting from Zero

#### Phase 1 — Sentry Basics
- [ ] Create Sentry account (start with free 14-day Business trial)
- [ ] Install `Sentry.AspNetCore` NuGet package
- [ ] Configure Sentry SDK with DSN, tracing, and release tracking
- [ ] Verify errors appear in Sentry dashboard

#### Phase 2 — GitHub Integration
- [ ] Connect GitHub repos: **Settings → Integrations → GitHub**
- [ ] Set up code mappings for stack trace linking
- [ ] Enable PR comments (merged + open PR comments)
- [ ] Test: commit with `Fixes SENTRY-XXXXX` in message to verify issue resolution

#### Phase 3 — Slack Integration
- [ ] Connect Slack workspace: **Settings → Integrations → Slack**
- [ ] Invite Sentry bot to your alert channels: `@sentry`
- [ ] Link personal account: `/sentry link`
- [ ] Create an issue alert rule with Slack notification action
- [ ] Test alert reaches the correct Slack channel

#### Phase 4 — Seer AI
- [ ] Start 14-day Seer trial: **Settings → Subscription → Start Trial (Seer)**
- [ ] Connect repos to Seer: **Settings → Seer → SCM Settings**
- [ ] Configure automation level per project: **Settings → Seer → Projects**
- [ ] Enable Autofix for PR creation on high-confidence issues
- [ ] Enable Code Review on selected repositories
- [ ] Test: trigger Autofix manually on a known issue

#### Phase 5 — GitHub Copilot Integration (for automated PRs)
- [ ] Ensure GitHub Copilot Enterprise / Cloud Agent is enabled in GitHub org
- [ ] Install GitHub Copilot integration in Sentry: **Settings → Integrations → GitHub Copilot**
- [ ] Authorize from a Sentry issue → **Start Root Cause Analysis** → **Set Up GitHub Copilot**
- [ ] Test: run RCA → select **Send to GitHub Copilot** → verify PR is created

#### Phase 6 — Sentry MCP (VS Code + GitHub Copilot)
- [ ] Add Sentry MCP to VS Code settings:
  ```json
  // settings.json
  "mcp.servers": {
    "sentry": { "type": "http", "url": "https://mcp.sentry.dev/mcp" }
  }
  ```
- [ ] Authenticate with OAuth when prompted
- [ ] Test: paste a Sentry issue URL in GitHub Copilot Chat → verify it fetches and analyzes

#### Phase 7 — Slack AI (Optional Upgrade)
- [ ] Evaluate if Business+ plan ($18/user/mo) justifies AI features for your team
- [ ] Enable Seer Agent as AI Assistant in Slack workspace settings
- [ ] Test: `@sentry` in a Slack channel with a production debugging question

---

## Feature Availability Matrix

| Feature | GitHub | GitLab | Azure DevOps |
|---------|--------|--------|-------------|
| Error monitoring (.NET) | ✅ | ✅ | ✅ |
| Slack alerts | ✅ | ✅ | ✅ |
| Suspect commits | ✅ | ✅ | ❌ |
| Stack trace linking | ✅ | ✅ | ❌ |
| Seer RCA (telemetry only) | ✅ | ✅ | ✅ |
| Seer Autofix + PR creation | ✅ Cloud only | ✅ Cloud only | ❌ |
| Seer Code Review | ✅ Cloud only | ✅ Cloud only | ❌ |
| GitHub Copilot Agent handoff | ✅ | ❌ | ❌ |
| Sentry MCP | ✅ | ✅ | ✅ (via IDE) |
| Autofix in Slack | ✅ | ✅ | N/A |

---

## Security & Privacy

### Sentry AI Data Practices

- **No model training on your data** (off by default; requires explicit consent to enable)
- **AI output is org-scoped** — visible only to authorized members of your organization
- **Trusted subprocessors only** — third-party AI providers are contractually prohibited from training on your data
- **Disable all AI features:** Settings → Organization → General Settings → toggle off *Enable Generative AI Features*
- **Disable only PR creation:** Settings → Seer → Advanced Settings → disable *Enable Code Generation*

### Slack AI Data Practices

- Customer data **never leaves Slack** for AI processing
- Slack **does not train LLMs** on customer data
- Runs on Slack's secure infrastructure under the same compliance standards
- Meets enterprise compliance: SOC 2, ISO 27001, GDPR, HIPAA (Enterprise+)

---

## Useful Links

| Resource | URL |
|---------|-----|
| Sentry Seer docs | https://docs.sentry.io/product/ai-in-sentry/seer/ |
| Seer Autofix docs | https://docs.sentry.io/product/ai-in-sentry/seer/autofix/ |
| Seer Code Review docs | https://docs.sentry.io/product/ai-in-sentry/seer/code-review/ |
| Sentry MCP server | https://mcp.sentry.dev/ |
| Sentry MCP GitHub | https://github.com/getsentry/sentry-mcp |
| Sentry + Slack docs | https://docs.sentry.io/integrations/notification-incidents/slack/ |
| GitHub Copilot Agent docs | https://docs.sentry.io/integrations/coding-agents/copilot/ |
| Sentry .NET SDK | https://docs.sentry.io/platforms/dotnet/ |
| Sentry pricing | https://sentry.io/pricing/ |
| Seer pricing details | https://docs.sentry.io/pricing/#seer-pricing |
| Slack AI features | https://slack.com/features/ai |
| Slack pricing | https://slack.com/pricing |
