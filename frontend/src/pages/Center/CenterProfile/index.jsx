import styled from '@emotion/styled';
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined';
import NotListedLocationIcon from '@mui/icons-material/NotListedLocation';
import PhoneIcon from '@mui/icons-material/Phone';
import { Avatar, Box, Breadcrumbs, Button, Divider, Stack, Tab, Tabs, Typography } from '@mui/material';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { formatter } from '~/utils/service';
import CenterIntroduction from './CenterIntroduction';
import CenterMedia from './CenterMedia';
import CenterRating from './CenterRating';
import CenterTeacher from './CenterTeacher';
import CenterLocation from './CenterLocation';
const StyledTabs = styled((props) => (
    <Tabs
        {...props}
        TabIndicatorProps={{ children: <span className="MuiTabs-indicatorSpan" /> }}
    />
))({
    '& .MuiTabs-indicator': {
        display: 'flex',
        justifyContent: 'center',
        backgroundColor: 'transparent',
    },
    '& .MuiTabs-indicatorSpan': {
        maxWidth: 40,
        width: '100%',
        backgroundColor: 'white',
    },
});
const StyledTab = styled((props) => <Tab disableRipple {...props} />)(
    ({ theme }) => ({
        textTransform: 'none',
        fontWeight: theme.typography.fontWeightRegular,
        fontSize: theme.typography.pxToRem(15),
        marginRight: theme.spacing(1),
        color: 'rgba(255, 255, 255, 0.7)',
        '&.Mui-selected': {
            color: '#fff',
        },
        '&.Mui-focusVisible': {
            backgroundColor: 'rgba(100, 95, 228, 0.32)',
        },
    }),
);
function CenterProfile() {

    const [tab, setTab] = useState('1');

    const handleChange = (event, newValue) => {
        setTab(newValue);
    };
    return (
        <Box>
            <Box sx={{
                background: `linear-gradient(to bottom, #f4f4f6, transparent),linear-gradient(to right, #4468f1, #c079ea)`,
                transition: 'height 0.5s ease',
                paddingX: "70px",
                pt: "20px",
                pb: "10px"
            }}>
                <Breadcrumbs aria-label="breadcrumb">
                    <Link underline="hover" color="inherit" href="/">
                        Trang chủ
                    </Link>
                    <Link
                        underline="hover"
                        color="inherit"
                        href="/material-ui/getting-started/installation/"
                    >
                        Danh sách trung tâm
                    </Link>
                    <Typography sx={{ color: 'text.primary' }}>Trung tâm</Typography>
                </Breadcrumbs>
                <Stack direction="row" mt={3}>
                    <Stack direction="row" sx={{
                        width: "60%",
                        gap: 2,
                        alignItems: "center"
                    }}>
                        <Avatar alt="Remy Sharp"
                            src="https://s3.ap-southeast-1.amazonaws.com/kiddihub-prod/images/-z281sLV2FP@1639025170.jpeg"
                            sx={{ width: "150px", height: "150px" }}
                        />
                        <Box>
                            <Typography variant='h4'>Trung tâm ANTL và GDHN Happy House CS3 - Trung Hòa</Typography>
                            <Typography mt={2}><LocationOnOutlinedIcon />Số 5 lô 1 ô c4 Nam Trung Yên, Trung Hoà, Quận Cầu Giấy, Hà Nội</Typography>
                        </Box>
                    </Stack>
                </Stack>
                <Box mt={5}>
                    <StyledTabs
                        value={tab}
                        onChange={handleChange}
                        aria-label="tab-center"
                    >
                        <StyledTab value="1" label="Giới Thiệu" />
                        <StyledTab value="2" label="Giáo viên" />
                        <StyledTab value="3" label="Thư viện" />
                        <StyledTab value="4" label="Vị Trí" />
                        <StyledTab value="5" label="Đánh giá" />
                    </StyledTabs>
                </Box>
            </Box>
            <Stack direction="row" sx={{
                px: "200px",
                mt: "40px",
                gap: 3
            }}>
                {1 === Number(tab) && <CenterIntroduction />}
                {2 === Number(tab) && <CenterTeacher />}
                {3 === Number(tab) && <CenterMedia />}
                {4 === Number(tab) && <CenterLocation />}
                {5 === Number(tab) && <CenterRating />}
                <Box sx={{ width: "35%" }}>
                    <Box sx={{
                        border: "1px solid gray", p: "20px",
                        borderRadius: "5px"
                    }}>
                        <Typography variant='h6'>Tổng quan</Typography>
                        <Box mt={2}>
                            <Typography sx={{ fontSize: "14px" }}>Học phí</Typography>
                            <Typography variant='h6' sx={{ color: "#f97316" }}>{formatter.format(3000000)}</Typography>
                        </Box>
                        <Box mt={2}>
                            <Typography sx={{ fontSize: "14px" }}>Độ tuổi học sinh, học viên</Typography>
                            <Typography variant='h6'>12 tháng - 10 tuổi</Typography>
                        </Box>
                        <Box mt={2}>
                            <Typography sx={{ fontSize: "14px" }}>Số điện thoại</Typography>
                            <Stack direction='row' gap={3} sx={{ alignItems: "center" }}>
                                <Typography variant='h6'>30404839439</Typography>
                                <Button startIcon={<PhoneIcon />}>Liên hệ</Button>
                            </Stack>
                        </Box>
                        <Box mt={2}>
                            <Typography sx={{ fontSize: "14px" }}>Địa chỉ</Typography>
                            <Typography variant='h6'>Số 5 lô 1 ô c4 Nam Trung Yên, Trung Hoà, Quận Cầu Giấy, Hà Nội</Typography>
                            <Button startIcon={<NotListedLocationIcon />}>Xem trên map</Button>
                        </Box>
                    </Box>
                </Box>
            </Stack>
            <Divider sx={{ width: "80%", margin: "auto", mt: "100px" }} />
        </Box>
    )
}

export default CenterProfile
