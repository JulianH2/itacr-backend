# Changelog

## [0.2.0](https://github.com/JulianH2/itacr-backend/compare/v0.1.0...v0.2.0) (2026-10-09)


### Added

* **core:** add result, errors, and validation base (ITACR-38) ([ab56e97](https://github.com/JulianH2/itacr-backend/commit/ab56e974d413a2b27bdaaf0b29c3c6e14075576e))
* **db:** add active orders procedure for the kitchen queue (ITACR-10) ([b2a2d93](https://github.com/JulianH2/itacr-backend/commit/b2a2d930050800128ed96acb877bfb0935885100))
* **db:** add dbup migrator with initial schema (ITACR-37) ([e77e8a1](https://github.com/JulianH2/itacr-backend/commit/e77e8a18339b4ab1788c5ee44c43c7fc6245839a))
* **db:** add dbup migrator with initial schema (ITACR-37) ([9312b58](https://github.com/JulianH2/itacr-backend/commit/9312b58636b5fa63bcb2ad4568d90998a3cdfb33))
* **db:** add order and dining table procedures (ITACR-9) ([d84ac28](https://github.com/JulianH2/itacr-backend/commit/d84ac284457be7bde399c1046ffb74c58e27e44d))
* **db:** add order status history table (ITACR-40) ([006f1d9](https://github.com/JulianH2/itacr-backend/commit/006f1d9cec34542e3be66ebe1252ffc4419301eb))
* **db:** add script to leave itacr_app with execute only (ITACR-36) ([a1ddd5d](https://github.com/JulianH2/itacr-backend/commit/a1ddd5d88bc978011451a5451fd609adf6f09959))
* **db:** add stored procedures for identity and catalog (ITACR-36) ([653a2b9](https://github.com/JulianH2/itacr-backend/commit/653a2b987c5cde5fc29759d9c022a9745ce47db1))
* **kitchen:** add the kitchen queue endpoint for kitchen and managers (ITACR-10) ([b7c03b1](https://github.com/JulianH2/itacr-backend/commit/b7c03b1d5c27f861aa5131a2735cf2452fc5abee))
* **kitchen:** add the kitchen queue use case (ITACR-10) ([dda6bb2](https://github.com/JulianH2/itacr-backend/commit/dda6bb2dd1170266ace2a0c973cb7f639b790776))
* **kitchen:** add the kitchen repository with dapper (ITACR-10) ([168ec06](https://github.com/JulianH2/itacr-backend/commit/168ec061ee193f5dd6be97cbcec45cbdfe82d632))
* **kitchen:** kitchen queue endpoint with active orders (ITACR-10) ([7b54301](https://github.com/JulianH2/itacr-backend/commit/7b54301582a330fe067bff2d963f6dd9b4c52bcc))
* **orders:** add create order and dining tables use cases (ITACR-9) ([ffbf24f](https://github.com/JulianH2/itacr-backend/commit/ffbf24fc02931f2cb168bb17985fdf99b230cfb4))
* **orders:** add create order and dining tables use cases (ITACR-9) ([8450f5e](https://github.com/JulianH2/itacr-backend/commit/8450f5e4131b0ee757a01c4a62c4c2ecec698982))
* **orders:** add order domain rules and status history (ITACR-40) ([a91f7d4](https://github.com/JulianH2/itacr-backend/commit/a91f7d4040be2d95d22252dc057c73a7b060671a))
* **orders:** add order status rules and history to the domain (ITACR-40) ([46a85df](https://github.com/JulianH2/itacr-backend/commit/46a85df0ccb59d9152d62018e3aa3807fba76952))


### Fixed

* **db:** qualify the order status history objects with dbo (ITACR-40) ([aad7b99](https://github.com/JulianH2/itacr-backend/commit/aad7b99de10c9c3036eab2ad0b6c529309538737))
* **db:** wait for sql server before creating the database and exit cleanly on errors (ITACR-36) ([1b9322a](https://github.com/JulianH2/itacr-backend/commit/1b9322a19d6b1f62f480f221f892a08132baac9e))
* **orders:** leave order id to the repository and check channel against table (ITACR-40) ([ae93a24](https://github.com/JulianH2/itacr-backend/commit/ae93a24a748870ca53810c4d173e995154d7ac19))


### Changed

* **core:** restructure solution into clean architecture (ITACR-35) ([c87c06a](https://github.com/JulianH2/itacr-backend/commit/c87c06a6540ad23701099dd001ed4fe2d699691b))
* **core:** split identity and catalog into use cases with one endpoint each (ITACR-38) ([c23aece](https://github.com/JulianH2/itacr-backend/commit/c23aecee2e01b94756b3eb6b5bfdd48f396d6c8c))
* **core:** split identity and catalog into use cases with one endpoint each (ITACR-38) ([d33feac](https://github.com/JulianH2/itacr-backend/commit/d33feac20e12af46af33f85afd5019b739d2f63e))
* **db:** move employee seeding to the migrator (ITACR-37) ([bd25893](https://github.com/JulianH2/itacr-backend/commit/bd258938a838cb31d74a062d35d3c3e937e0ed75))
* **db:** replace ef core with dapper repositories (ITACR-36) ([551c6a8](https://github.com/JulianH2/itacr-backend/commit/551c6a867f6ba38a28c710a74540e997c52097d6))
* **db:** replace ef core with dapper repositories (ITACR-36) ([76feff1](https://github.com/JulianH2/itacr-backend/commit/76feff1ccc9e3c5e47522a4d1d906b8861847e40))


### Documentation

* **core:** document use cases, results, and endpoints (ITACR-38) ([c3c3de6](https://github.com/JulianH2/itacr-backend/commit/c3c3de6979a8249aa254808671b116a7fdea68f0))
* **db:** add itacr_app permission checks after the switch (ITACR-37) ([ddfdfdd](https://github.com/JulianH2/itacr-backend/commit/ddfdfdddc9bc18216c6c68a25edfcb2ef7807d1e))
* **db:** document data access with dapper (ITACR-36) ([d2ebfb6](https://github.com/JulianH2/itacr-backend/commit/d2ebfb6a7d182293a018a3549d119fd63e7d3542))
* **db:** document the dbup database workflow (ITACR-37) ([b2214b2](https://github.com/JulianH2/itacr-backend/commit/b2214b2769991d86612757c5ee5e0467bd0f4d4c))
* **db:** document the order id handoff and the channel rule (ITACR-40) ([6aeaaae](https://github.com/JulianH2/itacr-backend/commit/6aeaaaec590b6a15d4082d28d03638c96e43a7e9))
* **db:** document the order lifecycle and status history (ITACR-40) ([b729415](https://github.com/JulianH2/itacr-backend/commit/b7294153c1db103bd5ca3624f0ddf5dcbd00d088))
* **kitchen:** document the kitchen queue endpoint (ITACR-10) ([98298f9](https://github.com/JulianH2/itacr-backend/commit/98298f94f6d1db55002dbf90d1bd0a5a95deedba))
* **orders:** document order idempotency and its procedure (ITACR-9) ([7574c20](https://github.com/JulianH2/itacr-backend/commit/7574c20d693c8597e79c9c2875c018d979f72ef8))
* **repo:** add the jwt secret to the local setup (ITACR-36) ([58d0783](https://github.com/JulianH2/itacr-backend/commit/58d07839cdbd9b32e419c2cd37a5a47c1c2416ff))
* **repo:** describe the new solution layout (ITACR-35) ([4ac92cc](https://github.com/JulianH2/itacr-backend/commit/4ac92cce7e33dedabf0961ab2d6feaab8331f46b))
* **repo:** document the backend architecture (ITACR-33) ([81fea5c](https://github.com/JulianH2/itacr-backend/commit/81fea5cc7486a6d3afd55a7d5940a51140a1f269))
* **repo:** document the backend architecture (ITACR-33) ([e92deb0](https://github.com/JulianH2/itacr-backend/commit/e92deb00b71ba3488732747a450c2fb9293ad5eb))

## 0.1.0 (2026-10-02)


### Added

* **auth:** add pin and password login, shift jwt, and pin reset requests (ITACR-7) ([74d3b10](https://github.com/JulianH2/itacr-backend/commit/74d3b1069c634cd32c1d47b327728b69cf338506))
* **auth:** add staff pin login, shift jwt, and pin reset requests (ITACR-7) ([6d918b7](https://github.com/JulianH2/itacr-backend/commit/6d918b795e41342540a2e79b05abf1166814f4a0))
* **catalog:** add catalog service with category and product queries (ITACR-8) ([6256b76](https://github.com/JulianH2/itacr-backend/commit/6256b76aa81e0082b81c42589c8c81c24c6497eb))
* **catalog:** add public catalog endpoints for categories and products (ITACR-8) ([98d6510](https://github.com/JulianH2/itacr-backend/commit/98d651034e5348a8ed4fe4e168b8c89ee06c8d7e))
* **catalog:** catalog endpoints and products by category function (ITACR-8) ([7929907](https://github.com/JulianH2/itacr-backend/commit/79299079e8a3f0dfc2adbe4c757b73f898f41bef))
* **core:** scaffold asp.net core api with database connection (ITACR-26) ([ae1dd21](https://github.com/JulianH2/itacr-backend/commit/ae1dd21ef5132faaa3a4b33eca1ca94b69b09544))
* **core:** scaffold asp.net core api with database connection (ITACR-26) ([50a3226](https://github.com/JulianH2/itacr-backend/commit/50a3226acba8739e262d2fc231fe897caed984e4))
* **db:** add fn_GetProductsByCategory function and migration (ITACR-8) ([2f8377d](https://github.com/JulianH2/itacr-backend/commit/2f8377d791b95fad42246f338ce839dd6c62a556))
* **db:** add idempotent seed data (ITACR-28) ([9f8a12c](https://github.com/JulianH2/itacr-backend/commit/9f8a12c6668a9b3b4d091431dbefc834eea5a400))
* **db:** add idempotent seed data (ITACR-28) ([cbcb868](https://github.com/JulianH2/itacr-backend/commit/cbcb868877125c16da9ff83af66b24b4bd69125a))
* **db:** add initial database schema (ITACR-27) ([0c7de39](https://github.com/JulianH2/itacr-backend/commit/0c7de39f40e8d707ecc2a8969274a6e800a190e3))
* **db:** add initial database schema (ITACR-27) ([612a59c](https://github.com/JulianH2/itacr-backend/commit/612a59c2aaad5d67dd30306b6d695e3ff68fbaf4))
* **db:** add pin lockout, pin reset subject, and login test employees (ITACR-7) ([e754e55](https://github.com/JulianH2/itacr-backend/commit/e754e5534960c18ce5e9e0be1c4a080e7c5f00af))


### Fixed

* **auth:** register the auth feature lost in the sprint-01 merge (ITACR-7) ([805556e](https://github.com/JulianH2/itacr-backend/commit/805556e6c88646139b616b00caedad96069b5bd0))
* **auth:** register the auth feature lost in the sprint-01 merge (ITACR-7) ([b48e818](https://github.com/JulianH2/itacr-backend/commit/b48e818304309cc59177f7ecdfef21d9b5ca02aa))


### Documentation

* **repo:** add readme, contributing guide, coding standards, and templates (ITACR-29) ([2838685](https://github.com/JulianH2/itacr-backend/commit/2838685c70c8aa08216a07526591ae5f1b9fa893))
* **repo:** add readme, contributing guide, coding standards, and templates (ITACR-29) ([a699746](https://github.com/JulianH2/itacr-backend/commit/a6997468dbe3d06084ead38497b40d841aea667d))
* **repo:** document the azure production resources, domains, and settings (ITACR-31) ([e965371](https://github.com/JulianH2/itacr-backend/commit/e96537128c9f3a2e6b16b27bf96edf446ea7b215))
* **repo:** document the deploy workflow and manual azure sql migrations (ITACR-32) ([a09f5ca](https://github.com/JulianH2/itacr-backend/commit/a09f5ca7365fd5f7e1ecb19501386f3705db4ca8))
* **repo:** prepare the backend for the azure production resources (ITACR-31) ([a1acbf0](https://github.com/JulianH2/itacr-backend/commit/a1acbf07e8b7584b742103fbceac43a495df3d10))

## Changelog

All notable changes to this project are documented in this file. It follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

This file is **generated by [release-please](https://github.com/googleapis/release-please)** from the Conventional Commits merged into `main`. Do not edit it by hand: write good commit messages instead. `feat` commits appear under "Added", `fix` under "Fixed", `perf`/`refactor` under "Changed", and `docs` under "Documentation".
