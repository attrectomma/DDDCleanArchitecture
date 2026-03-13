We are in an empty folder, this is not even a github or gitlab repository yet.

# Problem statement

We want to compare different levels of code for the same exact business logic to showcase concepts like Clean Architecture and Domain Driven Design.
So we will have multiple .NET Core WebApis and their respective integration test project so we can do a side by side comparison.
This will be used as educational material for people from intern level to mid medior level.

# Domain and Invariants
We have Projects. A Project can have multiple Retro boards. A retro board can have multiple columns. A column can have multiple notes. A note can have votes.
We also have users. Users are assigned to projects. For simplicity any user of a project can participate in any retro of said project.
Column names within a retro must be unique. Note names within a column must be unique. A user may cast only 1 vote per note.
Use an AudibleEntiryBase with stuff liek createdAt, lastUpdatedAt, deletedAt, use soft delete logic and EFCoreInterceptors
Use the unit of work pattern

# Web Apis to build

## API 1
No domain driven design, but Clean Architecture basics. No Mediator pattern.
In this version we do the following: database table : entity : repository : service : controller maps 1 to 1.
Example: Column table, Column Entity, IColumnRepository, IColumnService, ColumnController
This is not good practice, I know. But this is what you very frequently encounter from juniors. Business Logic lives in Service layer, domain models are anemic. Concurrency and consistency boundaries is not something the developers are aware of.

## API 2
Same as API 1, but push all business logic possible into domain models.

## API 3
Introduces Aggregate Design, but has a Project and a Retro aggregate. Gets rid of all the lower level repositories and services. Introduces the concept of a consistency boundary but also the risk of aggregate explosion.

## API 4
Splits the big Retro aggregate, extracting Vote as its own aggregate. Should show advantages regarding write contention, etc, but introduces the need for cross-aggregate checks.   

## API 5
Optional: if you have good ideas for this domain. Should be less noun and entity centric and focus on behaviour instead.
Add mediator pattern

# Tests

Docker will be running. All APIs must have the exact same integration tests that test end to end from calling endpoints to reading from postgres db running in docker. You must setup the integration test harness properly. Concurrency etc tests may fail in API1 and API2, but must pass in API3 and API4.

# Code Comment
All code must be commented in detail using .NET best practices for code comments.
Design decisions must be explained. "Higher Tier" APIs must explain why they are better than lower tiers and must also explain trade offs. This can be done in seperate .md files
  
