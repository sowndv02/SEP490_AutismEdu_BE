import ArrowBackIosNewIcon from '@mui/icons-material/ArrowBackIosNew';
import EscalatorWarningIcon from '@mui/icons-material/EscalatorWarning';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { LoadingButton } from '@mui/lab';
import { Box, FormControl, FormHelperText, IconButton, InputAdornment, InputLabel, OutlinedInput } from '@mui/material';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { enqueueSnackbar } from 'notistack';
import React, { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import HtmlTooltip from '~/components/HtmlTooltip';
import service from '~/plugins/services';
import checkValid from '~/utils/auth_form_verify';
import PAGES from '~/utils/pages';
function ResetPassword() {
    const [showPassword, setShowPassword] = useState(false);
    const [passwordError, setPasswordError] = useState(null);
    const [passwordConfirmError, setPasswordConfirmError] = useState(null);
    const [password, setPassword] = useState("");
    const [cfPassword, setCfPassword] = useState("");
    const location = useLocation();
    const urlParams = new URLSearchParams(location.search);
    const userId = urlParams.get('userId');
    const code = urlParams.get('code').replaceAll(" ", "+");
    const security = urlParams.get('security').replaceAll(" ", "+");
    const [loading, setLoading] = useState(false);
    const nav = useNavigate();
    const INPUT_CSS = {
        width: "100%",
        borderRadius: "15px"
    };

    useEffect(() => {
        if (loading) {
            handleSubmit();
        }
    }, [loading])

    const handleMouseDownPassword = (event) => {
        event.preventDefault();
    };
    const handleClickShowPassword = () => setShowPassword((show) => !show);

    const handleSubmit = async () => {
        if (passwordError !== null || passwordConfirmError !== null) {
            setLoading(false);
            return;
        } else {
            const checkPw = checkValid(password, 2, setPassword);
            const checkCfPw = checkValid(cfPassword, 3, setPasswordConfirmError, password);
            if (!checkPw || !checkCfPw) {
                setLoading(false);
                return;
            } else {
                await service.AuthenticationAPI.resetPassword({
                    code,
                    security,
                    userId,
                    password,
                    confirmPassword: cfPassword
                }, (res) => {
                    enqueueSnackbar("Đặt lại mật khẩu thành công!", { variant: "success" });
                    nav(PAGES.ROOT + PAGES.LOGIN);
                }, (err) => {
                    if (err.code === 500) {
                        enqueueSnackbar("Đặt lại mật khẩu thất bại!", { variant: "error" });
                    }
                    else enqueueSnackbar(err.error[0], { variant: "error" });
                })
                setLoading(false)
            }
        }
    }
    return (
        <Box sx={{ bgcolor: "#f7f7f9", height: "100vh", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <Card sx={{
                width: "450px",
                boxShadow: "rgba(0, 0, 0, 0.35) 0px 5px 15px",
                borderRadius: "10px",
                p: "28px"
            }}>
                <CardContent>
                    <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", gap: 1 }}>
                        <EscalatorWarningIcon sx={{ color: "#394ef4", fontSize: "40px" }} />
                        <Typography sx={{ fontSize: 20, fontWeight: "bold", color: "text.secondary" }}>
                            AutismEdu
                        </Typography>
                    </Box>
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Đặt lại mật khẩu 🔒</Typography>
                    <Typography sx={{ mt: "10px" }}>Your new password must be different from previously used passwords</Typography>
                    <Box mt="30px">
                        <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                            <InputLabel htmlFor="new-password">New Password</InputLabel>
                            <OutlinedInput
                                error={!!passwordError}
                                value={password}
                                id="new-password"
                                type={showPassword ? 'text' : 'password'}
                                onChange={(e) => {
                                    if (!e.target.value.includes(" ")) {
                                        checkValid(e.target.value, 2, setPasswordError);
                                        setPassword(e.target.value);
                                    }
                                }}
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
                                    <FormHelperText error id="password-error">
                                        <Box sx={{
                                            display: "flex",
                                            alignItems: "center",
                                            justifyContent: "space-between"
                                        }}>
                                            <p>{passwordError}</p>
                                            <HtmlTooltip
                                                title={
                                                    <React.Fragment>
                                                        <ul style={{ padding: "0", listStyle: "none" }}>
                                                            <li>Password length from 8 to 15 characters</li>
                                                            <li>Contains at least 1 number</li>
                                                            <li>Contains lowercase and uppercase letters</li>
                                                            <li>Contains at least one of the following special characters (. ! & %)</li>
                                                        </ul>
                                                    </React.Fragment>
                                                }
                                            >
                                                <HelpOutlineIcon sx={{ fontSize: "16px" }} />
                                            </HtmlTooltip>
                                        </Box>
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                        <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                            <InputLabel htmlFor="confirm-password">Confirm Password</InputLabel>
                            <OutlinedInput
                                error={!!passwordConfirmError}
                                value={cfPassword}
                                id="confirm-password"
                                type={showPassword ? 'text' : 'password'}
                                onChange={(e) => {
                                    if (!e.target.value.includes(" ")) {
                                        setCfPassword(e.target.value);
                                        checkValid(e.target.value, 3, setPasswordConfirmError, password);
                                    }
                                }}
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
                                passwordConfirmError && (
                                    <FormHelperText error id="cf-password-id">
                                        {passwordConfirmError}
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                    </Box>
                    <LoadingButton variant='contained' sx={{ width: "100%", marginTop: "20px" }} onClick={() => setLoading(true)}
                        loading={loading} loadingIndicator="Sending...">
                        Set New Password
                    </LoadingButton>
                    <Typography textAlign={'center'} mt="20px">
                        <Link to={PAGES.LOGIN} style={{ color: "#666cff" }}>
                            <ArrowBackIosNewIcon sx={{ fontSize: "12px" }} /> Back to login
                        </Link>
                    </Typography>

                </CardContent>
            </Card>
        </Box>
    );
}

export default ResetPassword;
