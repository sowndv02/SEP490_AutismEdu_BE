import { enqueueSnackbar } from 'notistack';
import React, { useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom';
import service from '~/plugins/services'
<<<<<<< HEAD
=======
import PAGES from '~/utils/pages';
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
function ConfirmRegister() {
    const location = useLocation();
    const urlParams = new URLSearchParams(location.search);
    const userId = urlParams.get('userId');
    const code = urlParams.get('code').replaceAll(" ", "+");
    const security = urlParams.get('security').replaceAll(" ", "+");
    const nav = useNavigate();
    useEffect(() => {
        handleSubmit();
    }, [])
    const handleSubmit = async () => {
<<<<<<< HEAD
        console.log({
            code,
            security,
            userId
        });
        try {
=======
        try {
            console.log(userId, code, security);
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
            await service.AuthenticationAPI.confirmEmail({
                code,
                security,
                userId
            }, (res) => {
<<<<<<< HEAD
                enqueueSnackbar("Confirm email successfully!", { variant: "success" });
                nav("/login")
            }, (err) => {
                console.log(err);
                if (err.code === 500) {
                    enqueueSnackbar("Failed to confirm your account!", { variant: "error" });
                }
                else enqueueSnackbar(err.error[0], { variant: "error" });
                nav("/login")
            })
        } catch (error) {
            enqueueSnackbar("Failed to confirm your account!", { variant: "error" });
            nav("/login")
=======
                console.log(res);
                enqueueSnackbar("Xác thực tài khoản thành công!", { variant: "success" });
                nav(PAGES.ROOT + PAGES.LOGIN)
            }, (err) => {
                console.log(err);
                if (err.code === 500) {
                    enqueueSnackbar("Xác thực tài khoản thất bại!", { variant: "error" });
                }
                else enqueueSnackbar(err.error[0], { variant: "error" });
                nav(PAGES.ROOT + PAGES.LOGIN)
            })
        } catch (error) {
            enqueueSnackbar("Xác thực tài khoản thất bại!", { variant: "error" });
            nav(PAGES.ROOT + PAGES.LOGIN)
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
        }
    }
    return (
        <></>
    )
}

export default ConfirmRegister
