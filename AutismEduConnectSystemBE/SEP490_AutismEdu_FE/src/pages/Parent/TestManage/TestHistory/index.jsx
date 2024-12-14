import { useState, useEffect } from 'react';
import { Box, Typography, Stack, TableContainer, Table, TableHead, TableRow, TableCell, TableBody, Paper, Button, Pagination, FormControl, InputLabel, Select, MenuItem, Dialog, DialogTitle, DialogContent, DialogActions, Divider, TextField, InputAdornment, Tooltip } from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import services from '~/plugins/services';
import TestResultDetail from '../TestResultDetail';
import { useNavigate } from 'react-router-dom';
import PAGES from '~/utils/pages';
import { format } from 'date-fns';

// const mockResultData = [
//     {
//         id: 3,
//         testId: 15,
//         testName: "Bai 3",
//         testDescription: "Bai 3.3",
//         totalPoint: 5,
//         createdDate: "2024-11-09T02:33:58.092895",
//     },
//     {
//         id: 2,
//         testId: 17,
//         testName: "Bai kiem tra 5",
//         testDescription: "Day la bai kiem tra 5.5",
//         totalPoint: 3,
//         createdDate: "2024-11-09T02:32:14.145402",
//     },
//     {
//         id: 1,
//         testId: 16,
//         testName: "Bai 4",
//         testDescription: "Bai 4.4",
//         totalPoint: 3,
//         createdDate: "2024-11-09T02:29:18.075121",
//     }
// ];

function TestHistory() {
    const nav = useNavigate();
    const [resultList, setResultList] = useState([]);
    const [loading, setLoading] = useState(false);
    const [testResultHistory, setTestResultHistory] = useState([]);
    const [selectedContent, setSelectedContent] = useState('');
    const [openDialog, setOpenDialog] = useState(false);
    const [filters, setFilters] = useState({
        search: '',
        orderBy: 'createdDate',
        sort: 'desc',
    });

    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 10,
        total: 10,
    });

    useEffect(() => {
        handleGetListTestHistory();
    }, [filters, pagination?.pageNumber]);

    const handleGetListTestHistory = async () => {
        try {
            setLoading(true);
            await services.TestResultManagementAPI.getListTestResultHistory((res) => {
                if (res?.result) {
                    setTestResultHistory(res.result);
                }
            }, (error) => {
                console.log(error);
            }, {
                search: filters.search,
                orderBy: filters.orderBy,
                sort: filters.sort,
                pageNumber: pagination?.pageNumber
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const handleFilterChange = (field) => (event) => {
        setFilters({ ...filters, [field]: event.target.value });
    };

    const handleOpenDialog = (content) => {
        setSelectedContent(content);
        setOpenDialog(true);
    };

    const handleCloseDialog = () => {
        setOpenDialog(false);
        setSelectedContent('');
    };

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);
    // return(<TestResultDetail/>)
    return (
        <Box sx={{
            height: testResultHistory.length <= 3 ? "calc(100vh - 64px)" : "auto",
        }}>
            <Stack py={5} direction="column" sx={{ width: "80%", margin: "auto", gap: 2 }}>
                <Typography variant="h4" sx={{ mb: 3 }} textAlign={'center'}>
                    Lịch sử bài kiểm tra
                </Typography>
                <Stack direction={'row'} justifyContent={'space-between'} alignItems="center" sx={{ width: "100%", mb: 2 }} spacing={3}>
                    <Stack direction={'row'} justifyContent={'space-between'} spacing={2} sx={{ flex: 1 }}>
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
                <TableContainer component={Paper} sx={{ boxShadow: 3, borderRadius: 2 }}>
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell sx={{ fontWeight: 'bold' }}>STT</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Tên bài kiểm tra</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Mô tả</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Tổng điểm</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Ngày tạo</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {testResultHistory.map((result, index) => (
                                <TableRow key={result.id} hover>
                                    <TableCell>{index + 1 + (pagination.pageNumber - 1) * pagination.pageSize}</TableCell>
                                    <TableCell>
                                        <Tooltip title={result?.testName || ''} placement="top">
                                            <Box sx={{
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                whiteSpace: 'nowrap',
                                                maxWidth: '200px',
                                                color: 'blue',
                                                textDecoration: 'underline',
                                                cursor: 'pointer'
                                            }} onClick={() => nav(PAGES.ROOT + '/test-detail/' + result?.id)}>
                                                {result?.testName}
                                            </Box>
                                        </Tooltip>
                                    </TableCell>
                                    <TableCell>
                                        <Box sx={{ display: 'inline-flex', alignItems: 'center' }}>
                                            <Box sx={{
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                whiteSpace: 'nowrap',
                                                maxWidth: 250
                                            }}>
                                                {result?.testDescription}
                                            </Box>
                                            {result?.testDescription?.length > 35 && (
                                                <Button
                                                    variant="text"
                                                    size="small"
                                                    onClick={() => handleOpenDialog(result?.testDescription)}
                                                    sx={{ textTransform: 'none', color: 'primary.main' }}
                                                >
                                                    Xem thêm
                                                </Button>
                                            )}
                                        </Box>
                                    </TableCell>
                                    <TableCell>{result?.totalPoint}</TableCell>
                                    <TableCell>
                                        {/* {result?.createdDate && new Date(result?.createdDate).toLocaleDateString()} */}
                                        {result?.createdDate && format(result?.createdDate, '(HH:mm) dd-MM-yyyy')}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>

                <Dialog open={openDialog} onClose={handleCloseDialog} maxWidth="md" fullWidth>
                    <DialogTitle textAlign={'center'}>Mô tả chi tiết</DialogTitle>
                    <Divider />
                    <DialogContent>
                        <Typography>{selectedContent}</Typography>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={handleCloseDialog} variant='outlined' color="primary">Đóng</Button>
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
            </Stack>
        </Box >
    );
}

export default TestHistory;
