import { Box, Stack, Typography } from '@mui/material'
import React from 'react'
import Logo from '../Logo'

function Footer() {
    return (
        <Stack direction='row' sx={{ px: "130px", py: "30px", gap:"100px" }}>
            <Box>
                <Logo sizeLogo="40px" sizeName="40px" />
                <Typography>Trung tâm kết nối trẻ tự kỉ hàng đầu Việt Nam</Typography>
            </Box>
            <Box>
                <Typography variant='h5' mb={4}>Thông tin liên lạc</Typography>
                <Typography fontSize={18}><b>Số điện thoại: </b> <a href='tel:473249329432'>473249329432</a></Typography>
                <Typography fontSize={18}><b>Số điện thoại: </b> <a href='mailto:autismedu@gmail.com'>autismedu@gmail.com</a></Typography>
                <Typography fontSize={18}><b>Địa chỉ: </b> Hoà Lạc, Hà Nội</Typography>
            </Box>
        </Stack>
    )
}

export default Footer
