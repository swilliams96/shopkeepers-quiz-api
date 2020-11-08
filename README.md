# Shopkeeper's Quiz API

## About
The Shopkeeper's Quiz is a realtime quiz app which tests Dota 2 players' in-game knowledge of the items, heroes and abilities. Questions are automatically kept up to date, meaning that unlike many other quizzes and games, even after a big update the answers will always remain accurate. This repository contains the source code for the API and the data collection services that generate the available question pool.


## Development
The API picks a handful of questions at random from the database and stores them in an in-memory cache. When users request the next questions in the queue, the app will serve these questions, picking new ones if there aren't enough on hand in the cache.

The in-game data is retrieved by classes that implement the `IScraper` interface. These classes are in control of where and how they retrieve the data necessary to generate questions from. These implementations may request data from a free public API, scrape data from a wiki, or retrieve it via any other available means (*though if you wish to do any web scraping, please ensure that you [do so ethically](https://towardsdatascience.com/ethics-in-web-scraping-b96b18136f01#5ab1:~:text=The%20Ethical%20Scraper)*). The data is automatically refreshed on a recurring schedule to ensure that the questions remain accurate even after the game is updated.

Once the data is stored in the database, the `QuestionsGenerationService` will generate a number of available questions along with the correct answer and a handful of convincing incorrect answers from the data. These are then stored in the database to be served up later by the API.


## Contributing
All contributions are welcome, whether you've noticed a bug or an issue, or want to add a new type of question. Either open an issue and describe what the problem is, or start a pull request and ensure that all tests pass.


## Copyright
Source code of this API is &copy; Sam Williams 2020.

The Dota 2 name and logo, as well as any in-game images, text or other content is &copy; Valve Corporation.