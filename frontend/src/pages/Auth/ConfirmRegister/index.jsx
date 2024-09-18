import { enqueueSnackbar } from 'notistack';
import React, { useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom';
import service from '~/plugins/services'
import PAGES from '~/utils/pages';
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
        try {
            console.log(userId, code, security);
            await service.AuthenticationAPI.confirmEmail({
                code,
                security,
                userId
            }, (res) => {
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
        }
    }
    return (
        <></>
    )
}

export default ConfirmRegister
