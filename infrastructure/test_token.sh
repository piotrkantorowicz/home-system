curl -X POST http://localhost:9000/application/o/token/ \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "username=admin" \
  -d "password=YourPassword123!" \
  -d "client_id=dQkxlc484o5U6OoILPpfrY4UztF8xt7lEmXtVWMU" \
  -d "client_secret=i6QagTw6sbcnzBXr2dkVFaD8rKfLVyvURpkH2iFKBfhy61UMOylS7DvWZ6iFNSv8BoG5HOozgu9dBPTlOyUlze5xk0tu9P6yOJKQIrHrRd93lovUl9DZQJSaWlSTu1w2" \
  -d "scope=openid email profile"

curl -X POST http://localhost:9000/application/o/token/ \        
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "code=7523f474d50043d2a47291e1c04141a2" \
  -d "client_id=dQkxlc484o5U6OoILPpfrY4UztF8xt7lEmXtVWMU" \
  -d "client_secret=i6QagTw6sbcnzBXr2dkVFaD8rKfLVyvURpkH2iFKBfhy61UMOylS7DvWZ6iFNSv8BoG5HOozgu9dBPTlOyUlze5xk0tu9P6yOJKQIrHrRd93lovUl9DZQJSaWlSTu1w2" \
  -d "redirect_uri=http://localhost:5000"