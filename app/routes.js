const express = require('express')
const router = express.Router()

// Utilities
// ---------

// TBC

// Routes
// ------

// Prototype home --------------------------------------------------------------
router.get("/", function (req, res) {

  // Unset everything
  req.session.data = {}
  res.redirect("launch");

})

// Version a index -------------------------------------------------------------
// Catch URL hackers trying to get to restart from /a
router.get("/a", function (req, res) {

  res.redirect("launch");

})

// Assessment / questions ------------------------------------------------------
// One question route to rule them all!
router.post(/([a])\/(short-assessment-q[0-9]*)/, function (req, res) {

  var qUrl = req.params[1];
  var qNum = Number(qUrl.substr(18));

  // Load the JSON data if not already in session
  if (!req.session.data['assessment-data']) {
    var fs = require("fs");
    var assessmentFile = fs.readFileSync("app/data/assessment.json");
    // Save JSON in session for use/manipulation
    req.session.data['assessment-data'] = JSON.parse(assessmentFile);
  }

  // Setup the assessment data
  var assessmentData = req.session.data['assessment-data'];
  var numQuestions = assessmentData['questions'].length;
  req.session.data['num-questions'] = numQuestions;

  // Calculate completion status
  var percComplete = 0;

  if (qNum === 1 && req.session.data['start-assessment']) {

    decCompleteExact = 0;

  } else if (qNum === numQuestions) {

    // decCompleteExact = 1; // Actually, don't ever show 100% during the assessment
    decCompleteExact = 0.99;

  } else if (!req.session.data['answer']) {

    decCompleteExact = req.session.data['assessment-status']['exact'];

  } else {

    decCompleteExact = qNum / numQuestions; // Exact decimal

  }

  // decCompleteRounded = Math.round((decCompleteExact) * 10) / 10; // 10% stages method
  decCompleteRounded = Math.round((decCompleteExact + 0.00001) * 100) / 100;
  percComplete = Math.round(decCompleteRounded * 100);

  req.session.data['assessment-status'] = {
    'exact': decCompleteExact,
    'rounded': decCompleteRounded,
    'percentage': percComplete
  }

  // Process outcome
  if (req.session.data['start-assessment']) {

    delete req.session.data['start-assessment'];
    req.session.data['question-num'] = qNum;

  } else if (!req.session.data['answer']) {

    req.session.data['error-missing'] = true;
    req.session.data['question-num'] = qNum;

  } else {

    // Unset the current answer/state
    delete req.session.data['answer'];
    delete req.session.data['error-missing'];

    if (qNum === numQuestions) {

      var assessmentComplete = true;

      req.session.data['assessment-status'] = {
        'exact': 1,
        'rounded': 1,
        'percentage': 100
      }

    } else {

      // Increment the question
      req.session.data['question-num'] = qNum + 1;

    }

  }

  // Redirect
  if (assessmentComplete) {

    res.redirect('short-complete');

  } else {

    res.redirect('short-assessment');

  }

})

// Assessment complete ---------------------------------------------------------
//router.post(/([a])\/(short-complete)/, function (req, res) {
//})

module.exports = router
