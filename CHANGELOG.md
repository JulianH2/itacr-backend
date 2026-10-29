# Changelog

## [0.5.0](https://github.com/JulianH2/itacr-backend/compare/v0.4.0...v0.5.0) (2026-10-29)


### Added

* **db:** add the delivery time and the ready pickup orders to the procedures (ITACR-16) ([97905bc](https://github.com/JulianH2/itacr-backend/commit/97905bce667aec64b9ff9d4503d074c0251d2861))
* **db:** add the push subscription index and the order ready notice procedures (ITACR-15) ([02145bf](https://github.com/JulianH2/itacr-backend/commit/02145bfe7704b408638056364d04a64cf403b070))
* **orders:** add the notification id to the link of the ready notice (ITACR-15) ([8d67a75](https://github.com/JulianH2/itacr-backend/commit/8d67a75e95a25bbc7cd7e0c920b5fd9767525e09))
* **orders:** add the use cases to deliver an order and to list the ready pickups (ITACR-16) ([0407998](https://github.com/JulianH2/itacr-backend/commit/0407998c9fc8f9019b0cfdb391bf4933a9591c63))
* **orders:** expose the endpoints to deliver an order and to list the ready pickups (ITACR-16) ([74d84cf](https://github.com/JulianH2/itacr-backend/commit/74d84cf307f624ee6cc335cc3fb87485d05ea382))
* **orders:** let a waiter mark a ready order as delivered (ITACR-16) ([138ced5](https://github.com/JulianH2/itacr-backend/commit/138ced5e991933dc8c1c1b3fcd4473377ed92c4a))
* **orders:** notify the waiter or customer when their order is ready (ITACR-15) ([bf87c55](https://github.com/JulianH2/itacr-backend/commit/bf87c556bd5b6e5bd3c3d0e6280f8f7f2c180236))
* **orders:** notify the waiter or customer when their order is ready (ITACR-15) ([1b6da3a](https://github.com/JulianH2/itacr-backend/commit/1b6da3a953b705a6702a66627a44dbdacba7f0ca))


### Fixed

* **db:** keep the ready time when an order is delivered (ITACR-16) ([2c6b54e](https://github.com/JulianH2/itacr-backend/commit/2c6b54e180673a000d9009628a44f3fe9934f84a))


### Documentation

* **orders:** document delivering an order (ITACR-16) ([df102ec](https://github.com/JulianH2/itacr-backend/commit/df102eca7663fd25b019da21a4f9e4f6d8fb754c))
* **orders:** document the order ready notices and the web push settings (ITACR-15) ([e9d7114](https://github.com/JulianH2/itacr-backend/commit/e9d7114f579cd9177cbcc163e6db2da4110238ae))

## [0.4.0](https://github.com/JulianH2/itacr-backend/compare/v0.3.0...v0.4.0) (2026-10-24)


### Added

* **db:** add active orders of a waiter and expose the waiter on the order detail (ITACR-14) ([800a8c1](https://github.com/JulianH2/itacr-backend/commit/800a8c14caab8f6f9037e20cc17402388c660609))
* **db:** add migration status and development rollback to the migrator (ITACR-52) ([83ed7f6](https://github.com/JulianH2/itacr-backend/commit/83ed7f6b1c93cff6734136f8af7f60ebab3df93b))
* **db:** add migration status and development rollback to the migrator (ITACR-52) ([e70f1e0](https://github.com/JulianH2/itacr-backend/commit/e70f1e005972ed4264fafd73240419696fbc16f2))
* **db:** point the seeded products to their webp photos (ITACR-48) ([71a06fd](https://github.com/JulianH2/itacr-backend/commit/71a06fd6e3a775cf2ea2f106ed88fea4fa45e6d7))
* **db:** point the seeded products to their webp photos (ITACR-48) ([c8cf870](https://github.com/JulianH2/itacr-backend/commit/c8cf8700bd161d0189fb5fac4708a5e1e4d84b59))
* **orders:** active orders of a waiter and the detail only for its waiter (ITACR-14) ([fa1f16d](https://github.com/JulianH2/itacr-backend/commit/fa1f16d8de5e2182f599bc5e351eab323330f39e))
* **orders:** add the active orders endpoint and show a waiter only their orders (ITACR-14) ([4330761](https://github.com/JulianH2/itacr-backend/commit/43307618e79a57b5097b4a54923649a3c404005d))
* **orders:** add the active orders use case and limit the detail to its waiter (ITACR-14) ([bb3455b](https://github.com/JulianH2/itacr-backend/commit/bb3455bced909e65b1ef104a4055074d7a93c8fa))
* **orders:** keep the device time of orders sent after being offline (ITACR-13) ([866688b](https://github.com/JulianH2/itacr-backend/commit/866688b0768e80b506a0a7f51c98653f6ceb217d))
* **orders:** keep the device time of orders sent after being offline (ITACR-13) ([657bebd](https://github.com/JulianH2/itacr-backend/commit/657bebdcdb1a4ea03007750cc455424c972811a0))
* **orders:** read the active orders of a waiter and the waiter of an order (ITACR-14) ([ac38005](https://github.com/JulianH2/itacr-backend/commit/ac38005cb98ee0691ba61737d1fee4d331d00d77))


### Fixed

* **db:** give old local test employees the test password in development (ITACR-47) ([6c4d430](https://github.com/JulianH2/itacr-backend/commit/6c4d430b5ae7ebd28ea9e8a36275313246d34bda))
* **db:** give old local test employees the test password in development (ITACR-47) ([89f76cb](https://github.com/JulianH2/itacr-backend/commit/89f76cb62e000a8242a906ad2ee4f05f8492913b))
* **repo:** pin third-party actions to a commit sha (ITACR-51) ([f099054](https://github.com/JulianH2/itacr-backend/commit/f099054936780717d94afce3d9011a41f77f7478))
* **repo:** pin third-party actions to a commit sha (ITACR-51) ([11e455a](https://github.com/JulianH2/itacr-backend/commit/11e455aba568dfde1e7fee80dc00cc1330da4177))


### Documentation

* **db:** document the migrator status, rollback and reapplication (ITACR-53) ([1416cb9](https://github.com/JulianH2/itacr-backend/commit/1416cb95d4772d35f1eef6866bc2880cf2bdb494))
* **db:** document the migrator status, rollback and reapplication (ITACR-53) ([19e00d2](https://github.com/JulianH2/itacr-backend/commit/19e00d2cebdd40d3b5bd8a49ed555cf164688b1c))
* **db:** document the product photo paths (ITACR-48) ([7e957f3](https://github.com/JulianH2/itacr-backend/commit/7e957f37a8064d6e96a57a220084325debfec8fd))
* **orders:** document the active orders endpoint and the waiter rule (ITACR-14) ([d238948](https://github.com/JulianH2/itacr-backend/commit/d238948933f21f5ab6413b5da92fdb0cefffa4c8))
* **repo:** document the branch rulesets (ITACR-46) ([2f00e56](https://github.com/JulianH2/itacr-backend/commit/2f00e56b73832d339b129bf70253c8b5d5c1c146))
* **repo:** document the branch rulesets (ITACR-46) ([6c11bb9](https://github.com/JulianH2/itacr-backend/commit/6c11bb9599d1b41c47381cb8f4a14b3bc9043a6b))
* **repo:** document the createdAt rule for offline orders (ITACR-13) ([6279981](https://github.com/JulianH2/itacr-backend/commit/6279981c331f4dde3b7122665e823ec0089d0802))

## [0.3.0](https://github.com/JulianH2/itacr-backend/compare/v0.2.0...v0.3.0) (2026-10-19)


### Added

* **db:** add pickup order constraints and procedures (ITACR-12) ([3f1ba03](https://github.com/JulianH2/itacr-backend/commit/3f1ba031e65462ae4cb96cda01ff44aa30f6eda1))
* **db:** add procedures to change an order status and read its detail and recent orders (ITACR-11) ([8fd5fec](https://github.com/JulianH2/itacr-backend/commit/8fd5fec7e8a3ac3bec1c7633a594f072085d1524))
* **db:** seed the test employees outside development with seed:testemployees (ITACR-43) ([5690191](https://github.com/JulianH2/itacr-backend/commit/5690191a6649ae605f7b26b2e78f6a29f0f985f4))
* **orders:** add the pickup order repository (ITACR-12) ([395fc1e](https://github.com/JulianH2/itacr-backend/commit/395fc1e714c05ff47dd38fb3aefe294694874b23))
* **orders:** add the pickup order to the domain (ITACR-12) ([e6db843](https://github.com/JulianH2/itacr-backend/commit/e6db84353e08003e695fcd2e13fe5769d8b52556))
* **orders:** add the pickup order use cases (ITACR-12) ([d6ac7c1](https://github.com/JulianH2/itacr-backend/commit/d6ac7c12c16fac9c99b6c3756ca17e96c4675e8b))
* **orders:** add the public pickup order endpoints with their rate limits (ITACR-12) ([acd5273](https://github.com/JulianH2/itacr-backend/commit/acd527338be7d2516c708d859552f413e4779a86))
* **orders:** advance and undo the kitchen status of an order (ITACR-11) ([b59026d](https://github.com/JulianH2/itacr-backend/commit/b59026d65c574ab99316488085224df50e4335bf))
* **orders:** change and undo the kitchen status and read the order detail and timeline (ITACR-11) ([91bb2be](https://github.com/JulianH2/itacr-backend/commit/91bb2beaa2aff464f46f43c136038bdc5b2bc822))
* **orders:** public pickup order endpoints for customers without an account (ITACR-12) ([a8fe5ee](https://github.com/JulianH2/itacr-backend/commit/a8fe5eec639e8b386604c1b574c0a8917215623c))


### Fixed

* **deps:** pin system.security.cryptography.pkcs 10.0.13 for two dos advisories (ITACR-44) ([474a0e5](https://github.com/JulianH2/itacr-backend/commit/474a0e56fef61be35f94f141a89ff56054990a89))
* **deps:** pin the patched system.security.cryptography.pkcs (ITACR-44) ([99db36e](https://github.com/JulianH2/itacr-backend/commit/99db36e2b1e746db0f857929768a9faa6fd85881))


### Documentation

* **db:** define the customer model for pickup orders (ITACR-42) ([6ad5129](https://github.com/JulianH2/itacr-backend/commit/6ad512929b69233bf2e74329754c98553e4e856b))
* **db:** document kitchen status changes, undo and the order timeline (ITACR-11) ([53c33e8](https://github.com/JulianH2/itacr-backend/commit/53c33e8ff612ec6acc1c7029e72fc22fd60f2edb))
* **db:** note the checks needed before the customer constraint (ITACR-42) ([a2e97bf](https://github.com/JulianH2/itacr-backend/commit/a2e97bfeb127717efa15666a1ed496851124b943))
* **orders:** define public endpoints, limits and pickup order rules (ITACR-42) ([2e1691c](https://github.com/JulianH2/itacr-backend/commit/2e1691c7b34fc10cd5c32340e62036daa5c44dd4))
* **orders:** define public endpoints, limits and pickup order rules (ITACR-42) ([c028e64](https://github.com/JulianH2/itacr-backend/commit/c028e648dd0d00dc987a8f3f35f9ccf909b9103e))
* **orders:** document the pickup order endpoints and constraints (ITACR-12) ([3abd4d1](https://github.com/JulianH2/itacr-backend/commit/3abd4d14c558243ac62815ba687459559a17d551))
* **orders:** restrict public status to pickup orders and note phone abuse (ITACR-42) ([8b5ab79](https://github.com/JulianH2/itacr-backend/commit/8b5ab791c96e21aeaac14a316af56a8534604214))
* **orders:** say that a repeated pickup id returns the order saved (ITACR-12) ([c737281](https://github.com/JulianH2/itacr-backend/commit/c73728168593069c28bfafe0de08888b5591e6e4))
* **repo:** document the azure configuration (ITACR-34) ([33f7ae3](https://github.com/JulianH2/itacr-backend/commit/33f7ae32524be2976d554ca610ddac3bc857c9ba))
* **repo:** document the azure configuration (ITACR-34) ([2ff8e28](https://github.com/JulianH2/itacr-backend/commit/2ff8e28d97204773a240ddd13162f124db6f0286))
* **repo:** document the qa environment and its deploy (ITACR-43) ([521a330](https://github.com/JulianH2/itacr-backend/commit/521a33013c13ffe7f649ed16712aa9c72ae5af95))
* **repo:** link the azure configuration from the readme and the guides (ITACR-34) ([ee1c233](https://github.com/JulianH2/itacr-backend/commit/ee1c2339eeb2cc740f29c2d0ed5746ea794752b8))

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
