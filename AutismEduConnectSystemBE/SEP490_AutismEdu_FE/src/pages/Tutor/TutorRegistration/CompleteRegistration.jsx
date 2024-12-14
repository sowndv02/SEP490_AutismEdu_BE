import { Box, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';
import img from '~/assets/images/completeregister.png';
import ButtonIcon from '~/components/ButtonComponent/ButtonIcon';
import PAGES from '~/utils/pages';
function CompleteRegistration() {

    return (
        <Stack direction="row" pt={5} justifyContent="space-between">
            <Box sx={{ width: "60%" }} px="40px">
                <Typography variant='h4' mt={5} color="#0bb30b">Đơn đăng ký của bạn đã được gửi thành công</Typography>
                <Typography mt={2} mb={5}>Vui lòng chờ hệ thống xét duyệt đơn của bạn trong vòng vài ngày. Thông tin đăng ký sẽ được gửi qua email mà bạn đã đăng ký</Typography>
                <Link to={PAGES.ROOT + PAGES.HOME}>
                    <ButtonIcon text={"VỀ TRANG CHỦ"} width="300px" height="50px" fontSize="15px" />
                </Link>
            </Box>
            <Box>
                <img src={img} style={{ maxHeight: "450px" }} />
            </Box>
        </Stack >
    )
}

export default CompleteRegistration
