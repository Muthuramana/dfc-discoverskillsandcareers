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

module.exports = router
