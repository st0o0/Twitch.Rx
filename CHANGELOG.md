# Changelog

## [0.2.0](https://github.com/st0o0/Twitch.Rx/compare/v0.1.0...v0.2.0) (2026-06-29)


### Features

* **deps:** Remove R3 dependency ([f9f6baf](https://github.com/st0o0/Twitch.Rx/commit/f9f6bafb4fe6dbe7a0a533d925daccc8ee3fc4f2))
* Enhance NuGet package metadata ([ee02d9f](https://github.com/st0o0/Twitch.Rx/commit/ee02d9f1fd75a72c73bac4f08cec3bb4e41578a6))

## [0.1.0](https://github.com/st0o0/Twitch.Rx/compare/v0.1.0...v0.1.0) (2026-06-29)


* remove release.yml and add dependabot configuration ([3077256](https://github.com/st0o0/Twitch.Rx/commit/30772566fd10cdacf2ad9d35148767d1447e8b63))


### Features

* add .env file support to example project ([f92e179](https://github.com/st0o0/Twitch.Rx/commit/f92e179dfccd85e2e803efc0376b0f533fe3d01b))
* add auth module with TwitchAuthHandler DelegatingHandler ([3a6f697](https://github.com/st0o0/Twitch.Rx/commit/3a6f697e231167d840feeb218acf5df8017fbc79))
* add Device Code Flow for automatic user token acquisition ([99d1ad9](https://github.com/st0o0/Twitch.Rx/commit/99d1ad9fca2466bc38b96fc378837f0187b8b969))
* add EventSub module with Transport, Router, and reconnection ([5c83b01](https://github.com/st0o0/Twitch.Rx/commit/5c83b01a0278809dfae193930724d152d2f9a495))
* add example project with standalone and DI usage ([aa32488](https://github.com/st0o0/Twitch.Rx/commit/aa3248853c019855938d4fc2196323353fb22b84))
* add Helix API layer with UsersEndpoint ([1da37d9](https://github.com/st0o0/Twitch.Rx/commit/1da37d9de5850a5a0437fe72550b4d18c0e5f462))
* add Helix Chat Send Message endpoint ([e21f75b](https://github.com/st0o0/Twitch.Rx/commit/e21f75b0a35f5f99ee925ca7b7b580b4a5b9011a))
* add options pattern with validation and configurable URLs ([77b8d5e](https://github.com/st0o0/Twitch.Rx/commit/77b8d5e04d3e9afd9055e6a4e4bee165bd739f6a))
* add TwitchRxBuilder, TwitchRxClient, and DI integration ([89c6a87](https://github.com/st0o0/Twitch.Rx/commit/89c6a87c60ccd649ee5172cbd195ff3f8944a884))
* **helix:** add ChannelPoints and Moderation endpoints ([e07b80f](https://github.com/st0o0/Twitch.Rx/commit/e07b80f3829c41dcfe20b5da65e081ce44a0b6fa))
* **helix:** add Channels and Chat endpoints ([a44e376](https://github.com/st0o0/Twitch.Rx/commit/a44e37604252c1c939e803145eec7c56e4533425))
* **helix:** add HelixEndpoint base class with error handling and pagination ([619a944](https://github.com/st0o0/Twitch.Rx/commit/619a944e7f436fb9566ddb166ed245e170e4f501))
* **helix:** add Polls, Predictions, Bits, Clips endpoints ([6319818](https://github.com/st0o0/Twitch.Rx/commit/6319818ae1e5449cdbd5338245150f85e5adbd26))
* **helix:** add remaining endpoints (Ads, Conduits, ContentClassification, Entitlements, Extensions, Goals, GuestStar, Raids, Schedule, Whispers) ([9a0eda4](https://github.com/st0o0/Twitch.Rx/commit/9a0eda4363440b2ef20ac371602c322d1630b6e9))
* **helix:** add Search, Teams, HypeTrain, Analytics, Charity endpoints ([540b840](https://github.com/st0o0/Twitch.Rx/commit/540b840ed8440063b0967ec6706a7ba34ebdb25b))
* **helix:** add Streams, Subscriptions, Games, Videos endpoints ([26a96aa](https://github.com/st0o0/Twitch.Rx/commit/26a96aa2ceab64019cc3b7c1c5fc99de9960958a))
* **helix:** add Users endpoint and ITwitchHelixApi facade ([dc66ecf](https://github.com/st0o0/Twitch.Rx/commit/dc66ecf0e42733b197d05ea12a73382e5e3c74c3))
* make example project runnable with env var configuration ([c53a765](https://github.com/st0o0/Twitch.Rx/commit/c53a765cff0ce012c7b45733a1e5b2a8b4fda3cb))
* migrate to xUnit v3 with Microsoft Testing Platform ([cac052b](https://github.com/st0o0/Twitch.Rx/commit/cac052bfdadadfbcc0c068642b1d6b08d492de41))
* scaffold solution with project structure ([dd35007](https://github.com/st0o0/Twitch.Rx/commit/dd3500799c642c14eb27b89c08bd9b3363ff0d2a))
* **twitch-rx:** add Poll EventSub events + Helix Polls API ([a517a9b](https://github.com/st0o0/Twitch.Rx/commit/a517a9b0645c82fce2e5da60ce267b281e70b874))


### Bug Fixes

* address production-readiness review findings ([d1da956](https://github.com/st0o0/Twitch.Rx/commit/d1da956c164aed69802e44484066d192be0048ab))
* **ci:** fix ([30cd3fd](https://github.com/st0o0/Twitch.Rx/commit/30cd3fd515b3b1bd4008fd6ba785840b6230e93e))
* **helix:** add PutAsync to base class, fix BlockAsync error handling ([de63191](https://github.com/st0o0/Twitch.Rx/commit/de63191ec44990790f130604b36e6a7779c95ef9))
* **helix:** catch JsonException instead of bare catch in EnsureSuccessAsync ([416cc76](https://github.com/st0o0/Twitch.Rx/commit/416cc761f933173f2fcc0a3468f9e4f2322579f5))
* improve auth error messages and clean up example output ([acf186c](https://github.com/st0o0/Twitch.Rx/commit/acf186ce7e2f5356f36c52f8af7bb04f4f11ae15))


### Documentation

* add README with logo in WebSocket.Rx style ([f7d3097](https://github.com/st0o0/Twitch.Rx/commit/f7d3097f37b27c4596b9bebf602a2277bc7b0afb))


### Refactoring

* Improve auth token retrieval logic ([ee3b490](https://github.com/st0o0/Twitch.Rx/commit/ee3b49006b2a7427c6ecbf7324aa16c9a13f37ae))
* migrate Api/ to Helix/, update client wiring and tests ([dbe090a](https://github.com/st0o0/Twitch.Rx/commit/dbe090adb7cc3587c38b4b85506d75f4bfb3282c))
* Remove unused observables from TwitchAuth ([c418641](https://github.com/st0o0/Twitch.Rx/commit/c41864142318933946adbeabf261152a1b982a8b))
