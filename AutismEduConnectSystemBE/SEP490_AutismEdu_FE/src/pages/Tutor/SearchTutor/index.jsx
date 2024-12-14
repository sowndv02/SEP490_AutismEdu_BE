import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import BookmarkBorderIcon from '@mui/icons-material/BookmarkBorder';
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined';
import LocalPhoneOutlinedIcon from '@mui/icons-material/LocalPhoneOutlined';
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined';
import { Box, Button, Card, CardActions, CardContent, CardMedia, Grid, IconButton, Link, Rating, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import ButtonComponent from '~/Components/ButtonComponent';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import FormSearch from './FormSearch/FormSearch';
import { enqueueSnackbar } from 'notistack';

function SearchTutor() {

    const navigate = useNavigate();
    const location = useLocation();
    const searchVal = location.state?.searchVal;
    const city = location.state?.city;
    const [searchCriteria, setSearchCriteria] = useState({
        searchValue: searchVal ?? "",
        address: city?.split("|")[1] ?? "",
        selectedRating: 0,
        ageRange: [0, 15]
    });

    const [isDataExist, setDataExist] = useState(false);

    const [loading, setLoading] = useState(false);

    const [tutors, setTutors] = useState([]);

    const [pagination, setPagination] = useState(null);

    const [selected, setSelected] = useState('grid');
    const [showFilters, setShowFilters] = useState(false);


    useEffect(() => {
        window.scrollTo(0, 0);
        handleGetTutor();
    }, [searchCriteria]);

    const handleGetTutor = async (isMore = false) => {

        try {
            const tutorData = {
                search: searchCriteria.searchValue,
                searchAddress: searchCriteria.address,
                reviewScore: searchCriteria.selectedRating,
                ageFrom: searchCriteria.ageRange[0],
                ageTo: searchCriteria.ageRange[1],
                pageNumber: isMore ? pagination.pageNumber : 1
            };
            console.log(tutorData);

            setLoading(true);
            await services.TutorManagementAPI.handleGetTutors((res) => {
                if (res?.result) {
                    setDataExist(res.result.length !== 0);
                    setTutors(prev => isMore ? [...prev, ...res.result] : res.result);
                    setPagination(res.pagination);
                }
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: "error" })
                console.log(error);
            }, tutorData);

        } catch (error) {
            console.log(error)
        } finally {
            setLoading(false);
        }
    };


    const handleFilterClick = () => {
        setShowFilters(!showFilters);
    };

    const handleSearch = () => {
        handleGetTutor();
    };

    function handleClick(event) {
        event.preventDefault();
        console.info('You clicked a breadcrumb.');
    }
    const handleShowMore = () => {
        setPagination(prev => ({ ...prev, pageNumber: (prev.pageNumber + 1) }));
        handleGetTutor(true);
    };


    const handleClickToProfile = (id) => {
        navigate(`/autismedu/tutor-profile/${id}`);
    };

    const getCity = (address) => {
        const city = address.split('|')[0];
        return city;
    };

    const breadcrumbs = [
        <Link underline="hover" key="1" color="inherit" href="/" onClick={handleClick}>
            Trang chủ
        </Link>,
        <Typography key="3" sx={{ color: 'rgb(107, 115, 133)' }}>
            Danh sách trung tâm
        </Typography>,
    ];

    const formatter = new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0,
    });


    return (
        <Box sx={{ height: 'auto' }}>
            <FormSearch selected={selected} setSelected={setSelected}
                showFilters={showFilters} handleSearch={handleSearch} handleFilterClick={handleFilterClick} searchCriteria={searchCriteria} setSearchCriteria={setSearchCriteria} city={city} />
            <Box sx={{ height: 'auto', position: "relative", top: '-150px', width: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>

                <Box sx={{ width: '100%', maxWidth: '1200px', mx: 'auto', mt: 5, display: 'flex', flexWrap: 'wrap', justifyContent: 'flex-start', gap: 3 }}>

                    {(tutors.length === 0 && !isDataExist) ? <Typography variant='subtitle1' textAlign={'center'} color={'white'}>Hiện không có gia sư để hiển thị</Typography> :
                        <>
                            {selected !== "list" ? (
                                tutors.map((t, index) => (
                                    <Box key={index} sx={{ display: 'flex', flexWrap: 'wrap', width: '380px', height: 'auto', padding: "20px", borderRadius: "5px", transition: "transform 0.3s ease, box-shadow 0.3s ease", '&:hover': { transform: "scale(1.05) translateY(-10px)", boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)" } }}>
                                        <Card sx={{ height: "620px", width: '100%' }}>
                                            <CardMedia sx={{ height: 240, objectPosition: "top", objectFit: "cover" }} image={t.imageUrl} title="apeople" />
                                            <CardContent>
                                                <Box sx={{ display: "flex", alignItems: "center" }} my={1}>
                                                    <Rating name="read-only" value={t.reviewScore} readOnly />
                                                    <Typography ml={2}>({t.totalReview} Đánh giá)</Typography>
                                                </Box>
                                                <Typography gutterBottom variant="h4" component="div" sx={{ fontSize: "26px" }}>{t.fullName}</Typography>
                                                <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                                    <LocationOnOutlinedIcon />
                                                    <Typography>{t?.address ? getCity(t.address) : 'Hồ Chí Minh'}</Typography>
                                                </Box>
                                                <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2, '&:hover': { color: "blue" } }}>
                                                    <LocalPhoneOutlinedIcon />
                                                    <a href={`tel:${t.phoneNumber}`}><Typography sx={{ '&:hover': { color: "blue" } }}>{t.phoneNumber}</Typography></a>
                                                </Box>
                                                <Box sx={{ display: "flex", gap: "15px", flexDirection: 'column' }}>
                                                    <Box sx={{ display: "flex", alignItems: "center", gap: "10px", mt: 2, '&:hover': { color: "blue" } }}>
                                                        <EmailOutlinedIcon />
                                                        <a href={`mailto:${t.email}`}><Typography sx={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', maxWidth: '280px', '&:hover': { color: "blue" } }}>{t.email}</Typography></a>
                                                    </Box>
                                                </Box>
                                                <Typography mt={4} variant='h5' color={'green'}>{formatter.format(t.priceFrom)} - {formatter.format(t.priceEnd)}<Typography component="span" variant='subtitle1' color={'gray'}> / buổi <small>({t?.sessionHours} tiếng)</small></Typography></Typography>
                                            </CardContent>
                                            <CardActions>
                                                <Button sx={{ fontSize: "20px" }} onClick={() => handleClickToProfile(t.userId)}>Tìm hiểu thêm <ArrowForwardIcon /></Button>
                                            </CardActions>
                                        </Card>
                                    </Box>
                                ))
                            ) : (
                                tutors.map((t, index) => (
                                    <Box key={index} sx={{ width: 'auto', padding: "20px", borderRadius: "5px", transition: "transform 0.3s ease, box-shadow 0.3s ease", '&:hover': { transform: "scale(1.05) translateY(-10px)", boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)" } }}>
                                        <Card sx={{ display: 'flex', justifyContent: 'center', minHeight: "400px", width: "1200px" }}>
                                            <CardMedia sx={{ width: '40%', height: 'auto', borderRadius: '8px' }} image={t.imageUrl} title="Hanoi" />
                                            <Box sx={{ display: 'flex', flexDirection: 'column', width: '60%', padding: "20px" }}>
                                                <CardContent sx={{ flexGrow: 1 }}>
                                                    <Box sx={{ display: "flex", alignItems: "center" }} my={1}>
                                                        <Rating name="read-only" value={t.reviewScore} readOnly />
                                                        <Typography ml={2}>({t.totalReview} Đánh giá)</Typography>
                                                    </Box>
                                                    <Typography gutterBottom variant="h4" component="div" sx={{ fontSize: "26px" }}>{t.fullName}</Typography>
                                                    <Box sx={{ display: "flex", alignItems: "center", gap: "5px", mt: 2 }}>
                                                        <LocationOnOutlinedIcon />
                                                        <Typography>{t?.address ? getCity(t.address) : 'Hồ Chí Minh'}</Typography>
                                                    </Box>
                                                    <Box sx={{ display: "flex", gap: "15px" }}>
                                                        <Box sx={{ display: "flex", alignItems: "center", gap: "10px", mt: 2, '&:hover': { color: "blue" } }}>
                                                            <LocalPhoneOutlinedIcon />
                                                            <a href={`tel:${t.phoneNumber}`}><Typography sx={{ '&:hover': { color: "blue" } }}>{t.phoneNumber}</Typography></a>
                                                        </Box>
                                                        <Box sx={{ display: "flex", alignItems: "center", gap: "10px", mt: 2, '&:hover': { color: "blue" } }}>
                                                            <EmailOutlinedIcon />
                                                            <a href={`mailto:${t.email}`}><Typography sx={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', maxWidth: '300px', '&:hover': { color: "blue" } }}>{t.email}</Typography></a>
                                                        </Box>
                                                    </Box>
                                                    <Typography mt={4} variant='h5' color={'green'}>{formatter.format(t.priceFrom)} - {formatter.format(t.priceEnd)}<Typography component="span" variant='subtitle1' color={'gray'}> / buổi <small>({t?.sessionHours} tiếng)</small></Typography></Typography>
                                                </CardContent>
                                                <CardActions>
                                                    <Button sx={{ fontSize: "20px" }} onClick={() => handleClickToProfile(t.userId)}>Tìm hiểu thêm <ArrowForwardIcon /></Button>
                                                </CardActions>
                                            </Box>
                                        </Card>
                                    </Box>
                                ))
                            )}</>
                    }

                </Box>

                <Box mt={5} sx={{ display: "flex", justifyContent: "center", width: '100%' }}>
                    {(Math.ceil(pagination?.total / pagination?.pageSize) > pagination?.pageNumber) && (
                        <ButtonComponent
                            action={handleShowMore}
                            width={"200px"}
                            height={"50px"}
                            text={"Xem thêm"}
                            borderRadius={"20px"}
                        />
                    )}
                </Box>
            </Box>

            <LoadingComponent open={loading} setOpen={setLoading} />
        </Box>
    );
}

export default SearchTutor;
