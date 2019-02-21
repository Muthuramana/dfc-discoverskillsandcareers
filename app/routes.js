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

// Resume assessment -----------------------------------------------------------
router.post(/([a])\/(resume-assessment)/, function (req, res) {

  var qNum = Math.floor((Math.random() * 20) + 1); // Random number between 1 and 20
  var redirectUrl = 'short-assessment-q' + qNum;
  req.session.data['resume-url'] = redirectUrl;

  res.redirect(redirectUrl);
  next

})

// Assessment / questions ------------------------------------------------------
// One question route to rule them all!
// Note: 999 generates a random qNum
router.post(/([a])\/(short-assessment-q[0-9]*)/, function (req, res) {

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

  // Process the question number
  var qUrl = req.params[1];
  var qNum = Number(qUrl.substr(18));

  // Calculate completion status
  var percComplete = 0;

  if (qNum === 1 && req.session.data['start-assessment']) {

    decCompleteExact = 0;

  } else if (qNum === numQuestions) {

    // decCompleteExact = 1; // Actually, don't ever show 100% during the assessment
    decCompleteExact = 0.99;

  } else if (qNum === 999) {

    qNum = Math.floor((Math.random() * numQuestions) + 1); // Random number between 1 and numQuestions
    decCompleteExact = qNum / numQuestions;

  } else if (!req.session.data['answer']) {

    decCompleteExact = req.session.data['assessment-status']['exact'];

  } else {

    decCompleteExact = qNum / numQuestions;

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

    req.session.data['last-answer'] = req.session.data['answer'];

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

// Save progress ---------------------------------------------------------------
router.post(/([a])\/(save-progress)/, function (req, res) {

  var refDate = new Date();

  var d = new Date();

  // Basic old month lookup. Swap this out for date library in future.
  var months = new Array();
  months[0] = "January";
  months[1] = "February";
  months[2] = "March";
  months[3] = "April";
  months[4] = "May";
  months[5] = "June";
  months[6] = "July";
  months[7] = "August";
  months[8] = "September";
  months[9] = "October";
  months[10] = "November";
  months[11] = "December";

  var refDateText = d.getDate() + " " + months[d.getMonth()] + " " + d.getFullYear() + ", " + d.getHours() + ":" + d.getMinutes();

  req.session.data['reference-date'] = refDateText;

  res.redirect('save-progress');

})

module.exports = router
