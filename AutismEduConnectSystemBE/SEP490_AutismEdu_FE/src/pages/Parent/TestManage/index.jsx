import { Box, Button, Card, CardContent, CardMedia, Grid, InputAdornment, Pagination, Stack, TextField, Typography, Modal, FormControl, InputLabel, Select, MenuItem } from '@mui/material';
import React, { useEffect, useState } from 'react';
import SearchIcon from '@mui/icons-material/Search';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import DoTestConfirmation from './TestModal/DoTestConfirmation';
function TestManagement() {
    const [loading, setLoading] = useState(false);
    const [selectedTest, setSelectedTest] = useState(null);
    const [testList, setTestList] = useState([]);
    const [openConfirm, setOpenConfirm] = useState(false);
    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 10,
        total: 10,
    });
    const [modalOpen, setModalOpen] = useState(false);
    const [modalDescription, setModalDescription] = useState('');
    const [filters, setFilters] = React.useState({
        search: '',
        orderBy: 'createdDate',
        sort: 'desc',
    });


    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const handleOpenModal = (description) => {
        setModalDescription(description);
        setModalOpen(true);
    };

    const handleCloseModal = () => {
        setModalOpen(false);
    };

    const handleFilterChange = (key) => (event) => {
        setFilters({
            ...filters,
            [key]: event.target.value,
        });
    };

    useEffect(() => {
        window.scrollTo(0, 0);
        handleGetListTest();
    }, [filters, pagination.pageNumber]);

    const handleGetListTest = async () => {
        try {
            setLoading(true);
            await services.TestManagementAPI.getListTest((res) => {
                if (res?.result) {
                    setTestList(res.result);
                    setPagination(res.pagination);
                }
            }, (error) => {
                console.log(error);
            }, {
                ...filters,
                pageNumber: pagination.pageNumber
            })
        } catch (error) {
            console.log(error);

        } finally {
            setLoading(false);
        }
    };

    const handleConfirm = (test) => {
        setOpenConfirm(true);
        setSelectedTest(test);
    };

    const handleClose = () => {
        setSelectedTest(null);
        setOpenConfirm(false);

    }

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);
    return (
        <Stack direction='column' sx={{
            width: "80%",
            margin: "auto",
            gap: 2,
        }}>
            <Typography variant='h4' textAlign={'center'} my={5}>Danh sách bài kiểm tra</Typography>

            <Stack direction={'row'} alignItems={'center'} gap={2} mb={5}>
                {/* <Box sx={{ width: "20%"}}/> */}
                <Box width={'80%'}>
                    <TextField
                        fullWidth
                        size='small'
                        label="Tìm kiếm"
                        value={filters.search}
                        onChange={handleFilterChange('search')}
                        sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                        InputProps={{
                            endAdornment: (
                                <InputAdornment position="end">
                                    <SearchIcon />
                                </InputAdornment>
                            ),
                        }}
                    />
                </Box>
                <Box width={"20%"}>
                    <FormControl fullWidth size='small'>
                        <InputLabel id="sort-select-label">Thứ tự</InputLabel>
                        <Select
                            labelId="sort-select-label"
                            value={filters.sort}
                            label="Thứ tự"
                            onChange={handleFilterChange('sort')}
                            sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                        >
                            <MenuItem value="asc">Tăng dần theo ngày</MenuItem>
                            <MenuItem value="desc">Giảm dần theo ngày</MenuItem>
                        </Select>
                    </FormControl>
                </Box>
            </Stack>

            <Grid container spacing={3} sx={{ flexWrap: 'wrap' }}>
                {testList.map((test, index) => (
                    <Grid item key={index} xs={12} sm={6} md={4}>
                        <Card
                            
                            sx={{
                                maxWidth: "100%",
                                transition: 'transform 0.3s ease-in-out, box-shadow 0.3s ease-in-out',
                                cursor: 'pointer',
                                '&:hover': {
                                    transform: 'scale(1.05)',
                                    boxShadow: '0px 10px 15px rgba(0, 0, 0, 0.2)',
                                },
                            }}
                        >
                            <CardMedia
                                component="img"
                                height="240"
                                image="https://vntesters.com/wp-content/uploads/2019/06/test.jpg"
                                alt="Exercise Icon"
                                onClick={() => handleConfirm(test)}
                            />
                            <CardContent>
                                <Typography
                                    variant="h6"
                                    component="div"
                                    sx={{
                                        display: '-webkit-box',
                                        WebkitLineClamp: 2,
                                        WebkitBoxOrient: 'vertical',
                                        overflow: 'hidden',
                                        textOverflow: 'ellipsis',
                                        height: '52px',
                                    }}
                                    onClick={() => handleConfirm(test)}
                                >
                                    {test.testName}
                                </Typography>
                                <Typography
                                    variant="body2"
                                    color="text.secondary"
                                    sx={{
                                        display: '-webkit-box',
                                        WebkitLineClamp: 1,
                                        WebkitBoxOrient: 'vertical',
                                        overflow: 'hidden',
                                        textOverflow: 'ellipsis',
                                        height: '40px',
                                    }}
                                >
                                    {test.testDescription}
                                </Typography>
                                {test?.testDescription?.length > 50 ? (
                                    <Button variant="text" size="small" color="primary" onClick={() => handleOpenModal(test.testDescription)}>
                                        Xem thêm nội dung
                                    </Button>
                                ) : <Button></Button>}
                            </CardContent>
                        </Card>
                    </Grid>
                ))}
            </Grid>

            {testList.length !== 0 && (<Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                <Pagination
                    count={totalPages}
                    page={pagination.pageNumber}
                    onChange={handlePageChange}
                    color="primary"
                />
            </Stack>)}
            <LoadingComponent open={loading} setOpen={setLoading} />

            <Modal
                open={modalOpen}
                onClose={handleCloseModal}
                sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center' }}
            >
                <Box sx={{ backgroundColor: 'white', padding: 4, maxWidth: 600, width: '100%' }}>
                    <Typography variant="h6" mb={2}>Nội dung</Typography>
                    <Typography variant="body2" color="text.secondary">{modalDescription}</Typography>
                    <Box mt={2} display={'flex'} justifyContent={'flex-end'}>
                        <Button variant="contained" onClick={handleCloseModal}>Đóng</Button>
                    </Box>
                </Box>
            </Modal>

            {openConfirm && selectedTest && <DoTestConfirmation open={openConfirm} handleClose={handleClose} selectedTest={selectedTest} />}
        </Stack>
    )
}

export default TestManagement