const express = require('express')
const router = express.Router()

if (process.env.NOTIFYAPIKEY) {
  var NotifyClient = require('notifications-node-client').NotifyClient,
    notify = new NotifyClient(process.env.NOTIFYAPIKEY);
}

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
router.post(/([a|b|c])\/(resume-assessment)/, function (req, res) {

  var qNum = Math.floor((Math.random() * 20) + 1); // Random number between 1 and 20
  var redirectUrl = 'short-assessment-q' + qNum;
  req.session.data['resume-url'] = redirectUrl;

  res.redirect(redirectUrl);
  next

})

// Short assessment / questions ------------------------------------------------
// One question route to rule them all!
// Note: 999 generates a random qNum
router.post(/([a|b|c])\/(filter-questions-q[0-9]*)/, function (req, res) {

  // Load the JSON data if not already in session
  if (!req.session.data['assessment-data']) {
    var fs = require("fs");
    var assessmentFile = fs.readFileSync("app/data/assessment.json");
    // Save JSON in session for use/manipulation
    req.session.data['assessment-data'] = JSON.parse(assessmentFile);
  }

  // Setup the assessment data
  var assessmentData = req.session.data['assessment-data'];
  var numQuestions = assessmentData['filter-questions'].length;
  req.session.data['num-questions'] = numQuestions;

  // Process the question number
  var qUrl = req.params[1];
  var qNum = Number(qUrl.substr(18));
  console.log(qNum)

  // Calculate completion status
  var percComplete = 0;

  if (qNum === 1 && req.session.data['filter-questions']) {
    console.log('a')

    decCompleteExact = 0;

  } else if (qNum === numQuestions) {
    console.log('b')
    decCompleteExact = 0.99;

  } else if (qNum === 999) {
    console.log('c')
    if (!req.session.data['reference-number']) {

      req.session.data['error-missing-reference-number'] = true;
      res.redirect('landing');

    } else {

      qNum = Math.floor((Math.random() * (numQuestions - 1)) + 1); // Random number between 1 and numQuestions - 1
      decCompleteExact = qNum / numQuestions;

    }

  } else if (!req.session.data['answer']) {
    console.log('d')
    decCompleteExact = 1;

  } else {
    console.log('e')
    decCompleteExact = qNum / numQuestions;

  }

  decCompleteRounded = Math.round((decCompleteExact + 0.00001) * 100) / 100;
  percComplete = Math.round(decCompleteRounded * 100);

  req.session.data['assessment-status'] = {
    'exact': decCompleteExact,
    'rounded': decCompleteRounded,
    'percentage': percComplete
  }

  console.log(req.session)

  // Process outcome
  if (req.session.data['filter-questions']) {

    delete req.session.data['filter-questions'];
    req.session.data['question-num'] = qNum;

  } else if (!req.session.data['answer']) {

    req.session.data['error-missing'] = true;
    req.session.data['question-num'] = qNum;

  } else {

    req.session.data['last-answer'] = req.session.data['answer'];

    // Unset the current answer/state
    delete req.session.data['answer'];
    delete req.session.data['error-missing'];
    delete req.session.data['error-missing-reference-number'];

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

    res.redirect('long-complete');

  } else {

    res.redirect('filter-questions');

  }

})

// Short assessment / questions ------------------------------------------------
// One question route to rule them all!
// Note: 999 generates a random qNum
router.post(/([a|b|c])\/(short-assessment-q[0-9]*)/, function (req, res) {

  // Load the JSON data if not already in session
  if (!req.session.data['assessment-data']) {
    var fs = require("fs");
    var assessmentFile = fs.readFileSync("app/data/assessment.json");
    // Save JSON in session for use/manipulation
    req.session.data['assessment-data'] = JSON.parse(assessmentFile);
  }

  // Setup the assessment data
  var assessmentData = req.session.data['assessment-data'];
  var numQuestions = assessmentData['short-questions'].length;
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

    if (!req.session.data['reference-number']) {

      req.session.data['error-missing-reference-number'] = true;
      res.redirect('landing');

    } else {

      qNum = Math.floor((Math.random() * (numQuestions - 1)) + 1); // Random number between 1 and numQuestions - 1
      decCompleteExact = qNum / numQuestions;

    }

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
    delete req.session.data['error-missing-reference-number'];

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

// Long assessment / questions -------------------------------------------------

// !!! WARNING !!! THIS IS PRETTY MUCH A COPY OF THE ABOVE ROUTE FOR THE SHORT
// ASSESSMENT. THIS IS NOT THE SMARTEST WAY TO DO THIS - IT'D BE BETTER TO HAVE
// BOTH ASSESSMENTS HANDLED BY ONE ROUTE (DRY) BUT I DON'T KNOW WHAT THE LONG
// ASSESSMENT ENTAILS SO FOR NOW I HAVE JUST DUPLICATED THE SHORT ROUTE FOR THE
// SAKE OF DEMONSTRATION

// One question route to rule them all!
// Note: 999 generates a random qNum
router.post(/([a|b|c])\/(long-assessment-q[0-9]*)/, function (req, res) {

  // Load the JSON data if not already in session
  if (!req.session.data['assessment-data']) {
    var fs = require("fs");
    var assessmentFile = fs.readFileSync("app/data/assessment.json");
    // Save JSON in session for use/manipulation
    req.session.data['assessment-data'] = JSON.parse(assessmentFile);
  }

  // Setup the assessment data
  var assessmentData = req.session.data['assessment-data'];
  var numQuestions = assessmentData['long-questions'].length;
  req.session.data['num-questions'] = numQuestions;

  // Process the question number
  var qUrl = req.params[1];
  var qNum = Number(qUrl.substr(17));

  // Calculate completion status
  var percComplete = 0;

  if (qNum === 1 && req.session.data['start-assessment']) {

    decCompleteExact = 0;

  } else if (qNum === numQuestions) {

    // decCompleteExact = 1; // Actually, don't ever show 100% during the assessment
    decCompleteExact = 0.99;

  } else if (qNum === 999) {

    if (!req.session.data['reference-number']) {

      req.session.data['error-missing-reference-number'] = true;
      res.redirect('landing');

    } else {

      qNum = Math.floor((Math.random() * (numQuestions - 1)) + 1); // Random number between 1 and numQuestions - 1
      decCompleteExact = qNum / numQuestions;

    }

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
    delete req.session.data['error-missing-reference-number'];

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

    res.redirect('long-complete');

  } else {

    res.redirect('long-assessment');

  }

})

// Assessment complete ---------------------------------------------------------
//router.post(/([a])\/(short-complete)/, function (req, res) {
//})

// Save progress ---------------------------------------------------------------
router.post(/([a|b|c])\/(save-progress)/, function (req, res) {

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
  req.session.data['referrer'] = req.get('Referrer').indexOf('filter-questions') == -1 ? 'short-assessment-q' : 'filter-questions-q';

  res.redirect('save-progress');

})

router.post(/([a|b|c])\/(notify)/, function (req, res) {

  if (notify) {
    notify.sendEmail(
      '5d6a3342-d226-4770-979b-97fd3f6f160d',
      req.body.address,
      {
        personalisation: {
          'first_name': 'Test Recipient',
          'application_number': '300241'
        },
        reference: ''
      }
    )
    // .then(response => console.log(response))
    .catch(err => console.error(err))

    res.redirect('short-results');

  }

});

module.exports = router
