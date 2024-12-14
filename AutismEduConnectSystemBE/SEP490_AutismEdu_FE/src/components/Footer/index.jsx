import { Box, Stack, Typography } from '@mui/material'
import Logo from '../Logo'

function Footer() {
    return (
        <Stack direction='row' sx={{ px: "130px", py: "30px", gap:"100px" }} borderTop={1} borderColor={'lightgrey'}>
            <Box>
                <Logo sizeLogo="40px" sizeName="40px" />
                <Typography>Trung tâm kết nối trẻ tự kỉ hàng đầu Việt Nam</Typography>
            </Box>
            <Box>
                <Typography variant='h5' mb={4}>Thông tin liên lạc</Typography>
                <Typography fontSize={18}><b>Số điện thoại: </b> <a href='tel:0999999999'>0999999999</a></Typography>
                <Typography fontSize={18}><b>Số điện thoại: </b> <a href='mailto:autism.edu.work@gmail.com'>autism.edu.work@gmail.com</a></Typography>
                <Typography fontSize={18}><b>Địa chỉ: </b> Hoà Lạc, Hà Nội</Typography>
            </Box>
        </Stack>
    )
}

export default Footer
