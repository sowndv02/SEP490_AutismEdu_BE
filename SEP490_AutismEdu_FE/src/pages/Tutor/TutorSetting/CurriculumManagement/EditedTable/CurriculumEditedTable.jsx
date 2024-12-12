import React, { useEffect, useState } from 'react';
import { Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Dialog, DialogContent, Typography, Paper, Box, Stack, FormControl, InputLabel, Select, MenuItem, Pagination, DialogTitle, Divider, DialogActions } from '@mui/material';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import emptyBook from '~/assets/images/icon/emptybook.gif'

function CurriculumEditedTable({ setShowTable }) {
    const [selectedContent, setSelectedContent] = useState('');
    const [openDialogR, setOpenDialogR] = useState(false);
    const [openDialog, setOpenDialog] = useState(false);
    const [currentContent, setCurrentContent] = useState('');
    const [curriculums, setCurriculums] = useState([]);
    const [loading, setLoading] = useState(false);
    const [filters, setFilters] = React.useState({
        status: 'all',
        orderBy: 'createdDate',
        sort: 'desc',
    });
    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 5,
        total: 5,
    });

    useEffect(() => {
        handleGetCurriculums();
    }, [filters, pagination.pageNumber]);


    const handleOpenDialogR = (content) => {
        setSelectedContent(content);
        setOpenDialogR(true);
    };

    const handleCloseDialogR = () => {
        setOpenDialogR(false);
        setSelectedContent('');
    };


    const handleGetCurriculums = async () => {
        try {
            setLoading(true);
            await services.CurriculumManagementAPI.getCurriculums((res) => {
                if (res?.result) {
                    setCurriculums(res.result);
                    setPagination(res?.pagination);
                }
            }, (error) => {
                console.log(error);
            }, {
                status: filters.status,
                orderBy: filters.orderBy,
                sort: filters.sort,
                pageSize: pagination?.pageSize,
                pageNumber: pagination?.pageNumber
            });

            // await services.CurriculumManagementAPI.getUpdateRequest((res) => {
            //     if (res?.result) {
            //         setCurriculums(res?.result);
            //         setPagination(res?.pagination);
            //     }
            // }, (error) => {
            //     console.log(error);
            // }, {
            //     status: filters.status,
            //     orderBy: filters.orderBy,
            //     sort: filters.sort,
            //     pageNumber: pagination?.pageNumber
            // })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    }

    const handleOpenDialog = (content) => {
        setCurrentContent(content);
        setOpenDialog(true);
    };

    const handleCloseDialog = () => {
        setOpenDialog(false);
    };

    const statusText = (status) => {
        switch (status) {
            case 0:
                return 'Từ chối';
            case 1:
                return 'Chấp nhận';
            case 2:
                return 'Chờ duyệt';
            default:
                return 'Không rõ';
        }
    };

    const handleFilterChange = (key) => (event) => {
        setFilters({
            ...filters,
            [key]: event.target.value,
        });
        setPagination({
            ...pagination,
            pageNumber: 1,
        });
    };

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    return (
        <Box>
            <Box>
                <Button mb={2} variant='contained' startIcon={<ArrowBackIcon />} onClick={() => setShowTable(false)}>Quay lại</Button>
            </Box>
            <Stack direction={'row'} spacing={2} my={3}>
                <Typography mb={4} variant='h4' width={"30%"}>Danh sách đã sửa</Typography>

                <Stack direction={'row'} width={"70%"} justifyContent={'flex-end'} gap={2}>
                    <Box sx={{ flexBasis: '10%' }}>

                        <FormControl fullWidth size='small'>
                            <InputLabel id="status-select-label">Trạng thái</InputLabel>
                            <Select
                                labelId="status-select-label"
                                value={filters.status}
                                label="Trạng thái"
                                onChange={handleFilterChange('status')}
                                sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                            >
                                <MenuItem value={'all'}>Tất cả</MenuItem>
                                <MenuItem value={'approve'}>Đã chấp nhận</MenuItem>
                                <MenuItem value={'pending'}>Đang chờ</MenuItem>
                                <MenuItem value={'reject'}>Từ chối</MenuItem>
                            </Select>
                        </FormControl>
                    </Box>

                    <Box sx={{ flexBasis: '10%' }}>
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
            </Stack>
            {curriculums.length === 0 ? <Box sx={{ textAlign: "center" }}>
                <img src={emptyBook} style={{ height: "200px" }} />
                <Typography>Hiện tại chưa có dữ liệu!</Typography>
            </Box> :
                <>
                    <TableContainer component={Paper} sx={{ boxShadow: 3, borderRadius: '8px', overflow: 'hidden' }}>
                        <Table sx={{ minWidth: 650 }}>
                            <TableHead>
                                <TableRow>
                                    <TableCell sx={{ fontWeight: 'bold', backgroundColor: '#f5f5f5' }}>STT</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold', backgroundColor: '#f5f5f5' }}>Độ tuổi</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold', backgroundColor: '#f5f5f5' }}>Nội dung</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold', backgroundColor: '#f5f5f5' }}>Phản hồi</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold', backgroundColor: '#f5f5f5' }}>Ngày cập nhật</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold', backgroundColor: '#f5f5f5' }}>Trạng thái</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {curriculums.map((curriculum, index) => (
                                    <TableRow key={index} sx={{ '&:nth-of-type(odd)': { backgroundColor: '#f9f9f9' } }}>
                                        <TableCell>{index + 1}</TableCell>
                                        <TableCell>{curriculum?.ageFrom} - {curriculum?.ageEnd} tuổi</TableCell>
                                        <TableCell sx={{ maxWidth: 300, whiteSpace: 'normal', wordWrap: 'break-word' }}>
                                            {curriculum?.description?.length > 150 ? (
                                                <>
                                                    <div dangerouslySetInnerHTML={{ __html: curriculum?.description?.slice(0, 150) + '...' }} />
                                                    <Button onClick={() => handleOpenDialog(curriculum?.description)} sx={{ textTransform: 'none', fontSize: '14px', color: '#1976d2', padding: 0 }}>
                                                        Xem thêm
                                                    </Button>
                                                </>
                                            ) : (
                                                <div dangerouslySetInnerHTML={{ __html: curriculum?.description }} />
                                            )}
                                        </TableCell>
                                        <TableCell>
                                            <Box sx={{ display: 'inline-flex', alignItems: 'center' }}>
                                                <Box sx={{
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis',
                                                    whiteSpace: 'nowrap',
                                                    maxWidth: 250
                                                }}>
                                                    {curriculum?.rejectionReason || 'Chưa có phản hồi'}
                                                </Box>
                                                {curriculum?.rejectionReason?.length > 35 && (
                                                    <Button
                                                        variant="text"
                                                        size="small"
                                                        onClick={() => handleOpenDialogR(curriculum?.rejectionReason)}
                                                        sx={{ textTransform: 'none', color: 'primary.main' }}
                                                    >
                                                        Xem thêm
                                                    </Button>
                                                )}
                                            </Box>

                                        </TableCell>
                                        <TableCell>{curriculum?.createdDate && new Date(curriculum.createdDate).toLocaleDateString()}</TableCell>
                                        <TableCell>
                                            <Button
                                                variant="outlined"
                                                color={
                                                    curriculum.requestStatus === 1 ? 'success' :
                                                        curriculum.requestStatus === 0 ? 'error' :
                                                            'warning'
                                                }
                                                size="small"
                                                sx={{ borderRadius: 2, textTransform: 'none' }}
                                            >
                                                {statusText(curriculum.requestStatus)}
                                            </Button>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>

                        <Dialog open={openDialog} onClose={handleCloseDialog}>
                            <DialogContent sx={{ padding: 2 }}>
                                <Typography dangerouslySetInnerHTML={{ __html: currentContent }} />
                            </DialogContent>
                        </Dialog>
                    </TableContainer>

                    <Dialog open={openDialogR} onClose={handleCloseDialogR} maxWidth="md" fullWidth>
                        <DialogTitle textAlign={'center'}>Mô tả chi tiết</DialogTitle>
                        <Divider />
                        <DialogContent>
                            <Typography>{selectedContent}</Typography>
                        </DialogContent>
                        <DialogActions>
                            <Button onClick={handleCloseDialogR} variant='outlined' color="primary">Đóng</Button>
                        </DialogActions>
                    </Dialog>

                    <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                        <Pagination
                            count={totalPages}
                            page={pagination.pageNumber}
                            onChange={handlePageChange}
                            color="primary"
                        />
                    </Stack>
                </>}
            <LoadingComponent open={loading} setOpen={setLoading} />
        </Box>

    );
}

export default CurriculumEditedTable;
