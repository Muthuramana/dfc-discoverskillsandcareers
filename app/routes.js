const express = require('express')
const router = express.Router()

// Utilities
// ---------

// TBC

// Routes
// ------
// Add your routes here - above the module.exports line


// All Service Models
// ------------------

router.get("/", function (req, res) {

  // Unset everything
  req.session.data = {}
  res.redirect("launch");

})

router.get("/a", function (req, res) {

  res.redirect("launch");

})

// One question route to rule them all!
router.post(/([a])\/(assessment-q[0-9]*)/, function (req, res) {

  var qUrl = req.params[1];
  var qNum = Number(qUrl.substr(12));

  // Load the JSON data if not already in session
  if (!req.session.data['assessment-data']) {
    var fs = require("fs");
    var assessmentFile = fs.readFileSync("app/data/assessment.json");
    // Save JSON in session for use/manipulation
    req.session.data['assessment-data'] = JSON.parse(assessmentFile);
  }

  // Setup the assessment data
  var assessmentData = req.session.data['assessment-data'];

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

    // Increment the question
    req.session.data['question-num'] = qNum + 1;

  }

  res.redirect('assessment-q');

})

module.exports = router
