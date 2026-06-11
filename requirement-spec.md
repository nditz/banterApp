# PROJECT.md

# World Cup Prediction Battle Platform

## Project Overview

Build a modern web application focused on the FIFA World Cup that allows casual sports fans to compete through predictions without requiring the complexity of fantasy football.

The platform should be easy enough for any age group to understand and use.

The core idea is:

* Users predict match outcomes.
* Users compete against friends in leagues.
* Users compare their predictions against journalists, pundits, podcasters, media analysts, and top-performing community members.
* Users earn points based on prediction accuracy.
* AI generates entertaining banter, memes, social content, and pundit-style analysis based on prediction performance.

The platform should feel like a combination of:

* Fantasy sports
* Social media sports banter
* Prediction markets
* Sports talk shows
* AI-generated content creation

---

# Vision

Sports bring together:

* Families
* Friends
* Coworkers
* Communities

Many people enjoy competition but do not have the time or interest to learn fantasy football mechanics.

This platform removes complexity while keeping competition.

Users simply answer:

* Home Win
* Draw
* Away Win

Or:

* Correct Score
* Double Chance (Win/Draw)
* Other supported prediction markets

Then compete against:

* Friends
* Public leagues
* Pundits
* Journalists
* Influencers
* Podcasters

The platform automatically generates entertaining content based on prediction results.

Examples:

* "You called Argentina beating Brazil while everyone else doubted them."
* "Your friend predicted a 3-0 win. The match ended 0-1. This prediction aged like milk."

The generated content should be fun, shareable, and social-media-friendly.

---

# Tech Stack

## Frontend

* React
* TypeScript
* Next.js
* Tailwind CSS
* Shadcn UI
* React Query

## Backend

* ASP.NET Core 9 Web API
* Clean Architecture
* CQRS Pattern
* Entity Framework Core

## Database

Supabase PostgreSQL

## Authentication

Supabase Auth

Supported providers:

* Email
* Google

Anonymous users supported through:

* Cookies
* Browser local storage
* Unique recovery code

## Hosting

Frontend:

* Vercel

Backend:

* Vercel Functions OR Azure App Service

Database:

* Supabase

Media Storage:

* Supabase Storage

---

# User Types

## Anonymous User

No registration required.

Can:

* Predict matches
* Join public competitions
* Receive unique recovery code
* View leaderboard

Limitations:

* Maximum 3 AI content generations
* Limited league participation
* Cannot upload custom profile

Storage:

* Cookie
* Local Storage
* Recovery Code

---

## Registered User

Can:

* Create leagues
* Join leagues
* Generate unlimited content
* Compare against friends
* Export content prompts
* View prediction history
* Track statistics

---

## Verified Adult User (18+)

Requirements:

* Google Account
* Age Verification Checkbox
* Terms Acceptance

Can:

* Enable Mature Banter Mode

Mature Mode allows:

* Stronger language
* More aggressive sports banter

Still prohibited:

* Hate speech
* Harassment
* Racism
* Threats

---

# Core Features

## 1. Match Predictions

Supported prediction types:

### Match Result

* Home Win
* Draw
* Away Win

### Double Chance

* Home or Draw
* Away or Draw
* Home or Away

### Correct Score

Examples:

* 2-1
* 1-1
* 3-0

### Goal Markets

Future enhancement.

---

## 2. Prediction Scoring System

Simple and easy to understand.

### Standard Result

Correct Outcome:

+3 Points

Incorrect:

0 Points

---

### Correct Score

Correct Score:

+7 Points

---

### Double Chance

Correct:

+2 Points

---

### Bonus

Perfect Match Day:

+5 Bonus Points

Perfect Group Stage:

+20 Bonus Points

---

# League System

Users can:

* Create private leagues
* Invite friends
* Join using invite codes

League features:

* Standings
* Weekly winners
* Monthly winners
* Season winners

---

# Pundit Leaderboards

Create special prediction profiles:

Examples:

* ESPN Analyst
* BBC Analyst
* Sky Sports Analyst
* Popular Podcasters
* Football Journalists

Store their predictions separately.

Users can compare:

"My predictions vs Stephen A style pundits"

"My predictions vs Football Journalists"

"My predictions vs Top Community Predictors"

---

# Social Feed

Central area of website.

A continuously scrolling timeline.

Displays:

* Top predictions
* Worst predictions
* AI-generated memes
* AI-generated banter
* Sports news
* Pundit reactions
* Match highlights summaries

Think:

Twitter + Reddit + ESPN + Fantasy League

---

# Homepage Layout

Three-column desktop layout.

---

## Left Panel

Prediction Center

Displays:

* Upcoming matches
* Predict buttons

Buttons:

* Home Win
* Draw
* Away Win

Quick prediction experience.

---

## Center Panel

Live Banter Feed

Infinite scrolling feed.

Content types:

* Memes
* Banter cards
* AI analysis
* Sports news
* Leaderboard highlights

Examples:

"You predicted Morocco to beat Spain. 92% of users disagreed. You were right."

"Your friend's prediction has entered the hall of shame."

---

## Right Panel

League & Rankings

Tabs:

### My League

Leaderboard

### Global

Top users

### Pundits

Analyst rankings

### Friends

Friend rankings

---

# AI Content Engine

The unique selling point of the platform.

---

## AI Match Analysis

Generate content using:

### Inputs

User Prediction

Actual Result

Match Statistics

Possession

Shots

Expected Goals

Cards

Player Ratings

Opponent Predictions

League Context

---

## Outputs

### Pundit Analysis

Example:

"You predicted a cagey draw, but France dominated possession and created three times the expected goals."

---

### Friendly Banter

Example:

"Your confidence in England was admirable. Unfortunately reality had other plans."

---

### Roast Mode

Example:

"Your prediction accuracy is currently being investigated by scientists."

---

### Praise Mode

Example:

"You are now outperforming 89% of the community."

---

## Meme Generation

Generate:

* Meme captions
* Meme templates
* Image prompts

Store generated prompts.

---

## Video Script Generation

Generate:

### TikTok Script

### YouTube Short Script

### Instagram Reel Script

Formats:

15 sec

30 sec

60 sec

---

# AI Integrations

Use provider abstraction.

Support:

## OpenAI

GPT Models

---

## Anthropic

Claude Models

---

## Google

Gemini Models

---

# Sports Data Integrations

Create provider abstraction layer.

Support:

## Match Data APIs

* API-Football
* Sportmonks
* Football Data API

Data required:

* Fixtures
* Results
* Statistics
* Team information
* Group standings

---

## Sports News APIs

Support:

* NewsAPI
* GNews
* Sports news providers

Feed into central timeline.

---

# Media & Pundit Data

Support importing predictions from:

* Journalists
* Podcasts
* Sports websites
* Media outlets

Store:

* Source
* Prediction
* Publication Date

Every imported prediction must contain attribution.

---

# Attribution Requirements

Every external source must show:

Source Name

Source URL

Publication Date

Author

Where available.

Display:

"Prediction sourced from ESPN"

"Analysis sourced from BBC Sport"

Never present external content as original content.

---

# Content Safety

Default mode:

PG Rated

Suitable for:

* Children
* Families
* Schools

Moderation pipeline:

AI Moderation API

Profanity Filter

Toxicity Detection

---

# Advertising

Integrate Google AdSense.

Locations:

Homepage feed

Sidebar

Between content cards

Requirements:

* Responsive ads
* Lazy loading
* Performance optimized

---

# Database Design

## Users

* Id
* Email
* DisplayName
* Avatar
* IsAdultVerified
* CreatedAt

---

## AnonymousUsers

* Id
* RecoveryCode
* CookieId

---

## Matches

* Id
* TeamA
* TeamB
* KickoffTime

---

## Predictions

* Id
* UserId
* MatchId
* PredictionType
* PredictionValue
* PointsAwarded

---

## Leagues

* Id
* Name
* InviteCode

---

## LeagueMembers

* LeagueId
* UserId

---

## Pundits

* Id
* Name
* Organization

---

## PunditPredictions

* PunditId
* MatchId
* Prediction

---

## GeneratedContent

* Id
* UserId
* Type
* Prompt
* Output
* CreatedAt

---

## NewsFeedItems

* Id
* Source
* Title
* Url
* PublishedAt

---

# API Endpoints

## Auth

/api/auth/login

/api/auth/register

---

## Matches

/api/matches

/api/matches/upcoming

/api/matches/results

---

## Predictions

/api/predictions/create

/api/predictions/update

/api/predictions/history

---

## Leagues

/api/leagues/create

/api/leagues/join

/api/leagues/standings

---

## Leaderboards

/api/leaderboards/global

/api/leaderboards/leagues

/api/leaderboards/pundits

---

## AI

/api/ai/analyze

/api/ai/banter

/api/ai/meme

/api/ai/video-script

---

## Feed

/api/feed

/api/feed/trending

---

# Future Roadmap

Phase 1

World Cup only

---

Phase 2

Premier League

Champions League

La Liga

Serie A

Bundesliga

MLS

---

Phase 3

Other Sports

Cricket

Rugby

NBA

NFL

Formula 1

UFC

---

# Non-Functional Requirements

Performance:

* Lighthouse score >90

Accessibility:

* WCAG AA

Mobile-first

Responsive

SEO optimized

Server-side rendering

Caching

Rate limiting

Monitoring

Error tracking

Analytics

---

# Success Metrics

Measure:

* Predictions submitted
* League creation rate
* Daily active users
* AI content generations
* Social shares
* Ad revenue
* Retention rate
* Prediction accuracy engagement

---

# MVP Launch Requirements

Must Have:

✅ Match predictions

✅ Scoring

✅ Leagues

✅ Leaderboards

✅ Anonymous users

✅ Supabase Auth

✅ AI banter generation

✅ AI analysis generation

✅ Sports data integration

✅ Sports news feed

✅ Attribution system

✅ Google Ads

✅ Mobile responsive UI

Nice To Have:

* Meme image generation
* Video generation
* Voice commentary
* Live watch parties

The MVP should be production-ready and optimized for a FIFA World Cup launch with scalability for hundreds of thousands of users.
