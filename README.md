Wordament-Solver
================

A bot that automatically plays and consistently wins at Microsoft Wordament

Click below to watch it in action:

[![](http://img.youtube.com/vi/2WpjlTkEMcA/0.jpg)](http://www.youtube.com/watch?v=2WpjlTkEMcA)

Written in WPF & C#.

I do not condone cheating so PLEASE UNDERSTAND the spirit of this project was purely the joy as a developer in overcoming the technical challenges of writing an AI that plays Wordament with no human intervention.

It first finds the browser window containing the Wordament tab, then takes a screenshot of it, looks for the tiles on the gameboard, performs OCR on each tile and finds all the sequential letter combinations that form valid words from a list of 170,000+ words. Once it has the list of playable words it iterates through them starting with the longest words first and moving the mouse over the tiles to play each the word. It detects whether a word was recognised or not by the game as well as other game states such as when the game accuses the player of guessing (too many invalid words in a short time), when the time runs out and when a new game starts.

Note:
You will need to have Tesseract installed on your machine for the bot to perform OCR on the tiles.
The bot will expect to find it under: C:\Program Files (x86)\Tesseract-OCR\Tesseract.exe
Though that location is configurable in the app.config file.

Have fun!

Copyright codeandcats.com 2013 
