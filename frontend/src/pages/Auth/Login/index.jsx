import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { Box, Divider, FormControl, FormHelperText, IconButton, InputAdornment, InputLabel, OutlinedInput, SvgIcon, TextField } from '@mui/material';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { useState } from 'react';
import TrelloIcon from '~/assets/trello.svg?react';
import GoogleIcon from '@mui/icons-material/Google';
import { Link } from 'react-router-dom';
import PAGES from '~/utils/pages';
import service from '~/plugins/services'

function Login() {
  const [showPassword, setShowPassword] = useState(false);
  const [emailError, setEmailError] = useState(null);
  const [passwordError, setPasswordError] = useState(null);
  const [email, setEmail] = useState()
  const INPUT_CSS = {
    width: "100%",
    borderRadius: "15px",
    ".MuiFormHelperText-root": {
      color: "red"
    }
  };

  const handleMouseDownPassword = (event) => {
    event.preventDefault();
  };
  const handleClickShowPassword = () => setShowPassword((show) => !show);

  const checkValid = (e, field) => {
    if (field === 1) {
      if (e.target.value === "") {
        setEmailError("Please enter email / username")
      } else if (e.target.value.length < 6) {
        setEmailError("Username must be more than 6 characters")
      } else {
        setEmailError(null)
      }
    }
    if (field === 2) {
      if (e.target.value === "") {
        setPasswordError("Please enter password")
      } else if (e.target.value.length < 6) {
        setPasswordError("Password must be more than 6 characters")
      } else {
        setPasswordError(null)
      }
    }
  }
  const handleSubmit = () => {
    service.AuthenticationAPI.getData({}, (res) => {
      console.log("data", res);
    }, (err) => {
      console.log(err);
    })
  }
  return (
    <Box sx={{ bgcolor: "#f7f7f9", height: "100vh", display: "flex", alignItems: "center", justifyContent: "center" }}>
      <Card sx={{
        width: "450px",
        height: "614px",
        boxShadow: "rgba(0, 0, 0, 0.35) 0px 5px 15px",
        borderRadius: "10px",
        p: "28px"
      }}>
        <CardContent>
          <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", gap: 1 }}>
            <SvgIcon component={TrelloIcon} inheritViewBox sx={{ color: 'blue' }} />
            <Typography sx={{ fontSize: 20, fontWeight: "bold", color: "text.secondary" }}>
              My App
            </Typography>
          </Box>
          <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Welcome to MyApp! 👋</Typography>
          <Typography sx={{ mt: "10px" }}>Please sign-in to your account and start the adventure</Typography>
          <Box mt="30px">
            <FormControl sx={{ ...INPUT_CSS }} variant="outlined">
              <InputLabel htmlFor="email">Email or Username</InputLabel>
              <OutlinedInput id="email" label="Email or username" variant="outlined" type='email'
                onChange={(e) => { checkValid(e, 1) }}
                error={emailError} />
              {
                emailError && (
                  <FormHelperText error id="accountId-error">
                    {emailError}
                  </FormHelperText>
                )
              }
            </FormControl>
            <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
              <InputLabel htmlFor="password">New Password</InputLabel>
              <OutlinedInput
                error={passwordError}
                id="password"
                type={showPassword ? 'text' : 'password'}
                onChange={(e) => { checkValid(e, 2) }}
                endAdornment={
                  <InputAdornment position="end">
                    <IconButton
                      aria-label="toggle password visibility"
                      onClick={handleClickShowPassword}
                      onMouseDown={handleMouseDownPassword}
                      edge="end"
                    >
                      {showPassword ? <VisibilityOff /> : <Visibility />}
                    </IconButton>
                  </InputAdornment>
                }
                label="Password"
              />
              {
                passwordError && (
                  <FormHelperText error id="accountId-error">
                    {passwordError}
                  </FormHelperText>
                )
              }
            </FormControl>
          </Box>
          <Box sx={{ width: "100%", textAlign: "end", marginTop: "15px" }}>
            <Link to={PAGES.FORGOTPASSWORD} style={{ color: "#666cff" }}>Forgot Password?</Link>
          </Box>
          <Button variant='contained' sx={{ width: "100%", marginTop: "20px" }}>Sign In</Button>

          <Typography sx={{ textAlign: "center", mt: "20px" }}>New on our platform? <Link to={PAGES.REGISTER} style={{ color: "#666cff" }}>Create an account</Link></Typography>
          <Divider sx={{ mt: "15px" }}>or</Divider>
          <Box sx={{ display: "flex", justifyContent: "center" }}>
            <IconButton>
              <GoogleIcon sx={{ color: "#dd4b39 " }} />
            </IconButton>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}

export default Login;
